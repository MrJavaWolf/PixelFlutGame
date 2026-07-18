using Silk.NET.OpenCL;
using System.Text;

namespace pixelflut.TestShader;

public sealed class OpenClProgram : IDisposable
{
    private readonly object _gate = new();

    private CL? _cl;

    private nint _context;
    private nint _commandQueue;
    private nint _program;
    private nint _kernel;
    private nint _outputBuffer;

    private int _disposeState;

    /// <summary>
    /// Maximum number of float elements that can be processed in one call.
    /// </summary>
    public int InputBufferSize { get; }

    /// <summary>
    /// Creates the OpenCL context, compiles the program and allocates the
    /// input/output buffers.
    /// </summary>
    public OpenClProgram(string kernelSource, int inputBufferSize, string kernelName = "process_buffer")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kernelSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(kernelName);

        if (inputBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputBufferSize),
                inputBufferSize,
                "The input buffer size must be greater than zero.");
        }

        InputBufferSize = inputBufferSize;
        _cl = CL.GetApi();

        try
        {
            Initialize(kernelSource, kernelName);
        }
        catch
        {
            // Clean up resources that may have been created before the error.
            Dispose();
            throw;
        }
    }

    private void Initialize(string kernelSource, string kernelName)
    {
        CL cl = GetCl();

        nint device = FindDevice(cl);

        _context = CreateContext(cl, device);

        _commandQueue = cl.CreateCommandQueue(
            _context,
            device,
            CommandQueueProperties.None,
            out int error);

        Check(error, "clCreateCommandQueue");

        nuint kernelSourceLength = 0;

        _program = cl.CreateProgramWithSource(
            _context,
            1,
            [kernelSource],
            ref kernelSourceLength,
            out error);

        Check(error, "clCreateProgramWithSource");

        error = cl.BuildProgram(
            _program,
            1,
            [device],
            "-cl-std=CL1.2",
            null!,
            Array.Empty<byte>());

        if (error != 0)
        {
            string buildLog = GetBuildLog(
                cl,
                _program,
                device);

            throw new OpenClException(
                "clBuildProgram",
                error,
                buildLog);
        }

        _kernel = cl.CreateKernel(
            _program,
            kernelName,
            out error);

        Check(error, "clCreateKernel");

        nuint capacityInBytes = checked((nuint)InputBufferSize);

        Span<int> errorSpan = stackalloc int[1];
        _outputBuffer = cl.CreateBuffer(
            _context,
            MemFlags.WriteOnly,
            capacityInBytes,
            Span<byte>.Empty,
            errorSpan);

        Check(errorSpan[0], "clCreateBuffer(output)");

        /*
         * These arguments never change, so set them once.
         */

        // Argument 1: __global byte* output
        Check(
            cl.SetKernelArg(
                _kernel,
                0,
                (nuint)IntPtr.Size,
                in _outputBuffer),
            "clSetKernelArg(output)");
    }

    public void Run(
        Span<byte> output,
        int width,
        int height,
        float rainbow_scale,
        float offset)
    {
        ThrowIfDisposed();
        CL cl = GetCl();

        nuint byteCount = checked((nuint)output.Length);


        // Argument 1: int width
        Check(
            cl.SetKernelArg(
                _kernel,
                1,
                sizeof(int),
                in width),
            "clSetKernelArg(width)");

        // Argument 2: int height
        Check(
            cl.SetKernelArg(
                _kernel,
                2,
                sizeof(int),
                in height),
            "clSetKernelArg(height)");

        // Argument 3: float rainbow_scale
        Check(
            cl.SetKernelArg(
                _kernel,
                3,
                sizeof(float),
                in rainbow_scale),
            "clSetKernelArg(rainbow_scale)");

        // Argument 4: float offset
        Check(
            cl.SetKernelArg(
                _kernel,
                4,
                sizeof(float),
                in offset),
            "clSetKernelArg(offset)");

        Span<nuint> globalWorkSize = [byteCount / 3];
        Check(
            cl.EnqueueNdrangeKernel(
                _commandQueue,
                _kernel,
                work_dim: 1,
                global_work_offset: ReadOnlySpan<nuint>.Empty,
                global_work_size: globalWorkSize,
                local_work_size: ReadOnlySpan<nuint>.Empty,
                num_events_in_wait_list: 0,
                event_wait_list: ReadOnlySpan<nint>.Empty,
                @event: Span<nint>.Empty),
            "clEnqueueNDRangeKernel");

        /*
         * The blocking read waits for the preceding kernel execution
         * and copies only the active output range.
         */
        Check(
            cl.EnqueueReadBuffer(
                _commandQueue,
                _outputBuffer,
                blocking_read: true,
                offset: 0,
                size: byteCount,
                ptr: output,
                num_events_in_wait_list: 0,
                event_wait_list: ReadOnlySpan<nint>.Empty,
                @event: Span<nint>.Empty),
            "clEnqueueReadBuffer");
    }

    private CL GetCl()
    {
        return _cl ??
            throw new ObjectDisposedException(
                nameof(OpenClProgram));
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposeState) != 0)
        {
            throw new ObjectDisposedException(
                nameof(OpenClProgram));
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (Interlocked.Exchange(
                    ref _disposeState,
                    1) == 0)
            {
                ReleaseResources();
            }
        }

        GC.SuppressFinalize(this);
    }

    ~OpenClProgram()
    {
        try
        {
            if (Interlocked.Exchange(
                    ref _disposeState,
                    1) == 0)
            {
                ReleaseResources();
            }
        }
        catch
        {
            // Exceptions must not escape a finalizer.
        }
    }

    private void ReleaseResources()
    {
        CL? cl = _cl;

        if (cl is null)
            return;

        /*
         * Run normally leaves no outstanding work because its read is
         * blocking. Finish is useful if an exception occurred after a
         * command was enqueued but before the blocking read.
         */
        if (_commandQueue != 0)
            _ = cl.Finish(_commandQueue);

        if (_outputBuffer != 0)
        {
            _ = cl.ReleaseMemObject(_outputBuffer);
            _outputBuffer = 0;
        }

        if (_kernel != 0)
        {
            _ = cl.ReleaseKernel(_kernel);
            _kernel = 0;
        }

        if (_program != 0)
        {
            _ = cl.ReleaseProgram(_program);
            _program = 0;
        }

        if (_commandQueue != 0)
        {
            _ = cl.ReleaseCommandQueue(_commandQueue);
            _commandQueue = 0;
        }

        if (_context != 0)
        {
            _ = cl.ReleaseContext(_context);
            _context = 0;
        }

        _cl = null;
        cl.Dispose();
    }

    private static nint CreateContext(
        CL cl,
        nint device)
    {
        Span<int> error = stackalloc int[1];

        nint context = cl.CreateContext(
            properties: ReadOnlySpan<nint>.Empty,
            num_devices: 1,
            devices: [device],
            pfn_notify: null!,
            user_data: Span<byte>.Empty,
            errcode_ret: error);

        Check(error[0], "clCreateContext");

        return context;
    }

    private static nint FindDevice(CL cl)
    {
        nint[] platforms = new nint[16];
        uint[] platformCount = new uint[1];

        Check(
            cl.GetPlatformIDs(
                (uint)platforms.Length,
                platforms,
                platformCount),
            "clGetPlatformIDs");

        if (platformCount[0] == 0)
        {
            throw new InvalidOperationException("No OpenCL platforms were found.");
        }

        int availablePlatformCount = checked(
            (int)Math.Min(
                platformCount[0],
                (uint)platforms.Length));

        nint device = FindDeviceOfType(cl, platforms.AsSpan(0, availablePlatformCount), DeviceType.Gpu);

        if (device != 0)
            return device;

        device = FindDeviceOfType(
            cl,
            platforms.AsSpan(0, availablePlatformCount),
            DeviceType.All);

        if (device != 0)
            return device;

        throw new InvalidOperationException(
            "No usable OpenCL device was found.");
    }

    private static nint FindDeviceOfType(
        CL cl,
        ReadOnlySpan<nint> platforms,
        DeviceType deviceType)
    {
        foreach (nint platform in platforms)
        {
            nint[] devices = new nint[32];
            uint[] deviceCount = new uint[1];

            int error = cl.GetDeviceIDs(
                platform,
                deviceType,
                (uint)devices.Length,
                devices,
                deviceCount);

            if (error != 0 || deviceCount[0] == 0)
                continue;

            return devices[0];
        }

        return 0;
    }

    private static string GetBuildLog(
        CL cl,
        nint program,
        nint device)
    {
        Span<nuint> logSize = stackalloc nuint[1];

        int error = cl.GetProgramBuildInfo(
            program,
            device,
            ProgramBuildInfo.BuildLog,
            param_value_size: 0,
            param_value: Span<byte>.Empty,
            param_value_size_ret: logSize);

        if (error != 0)
        {
            return
                $"Could not retrieve the build-log size. " +
                $"OpenCL error: {error}.";
        }

        if (logSize[0] == 0)
            return "The OpenCL compiler returned no build log.";

        byte[] logBytes =
            new byte[checked((int)logSize[0])];

        error = cl.GetProgramBuildInfo(
            program,
            device,
            ProgramBuildInfo.BuildLog,
            (nuint)logBytes.Length,
            logBytes,
            Span<nuint>.Empty);

        if (error != 0)
        {
            return
                $"Could not retrieve the build log. " +
                $"OpenCL error: {error}.";
        }

        return Encoding.UTF8
            .GetString(logBytes)
            .TrimEnd('\0', '\r', '\n');
    }

    private static void Check(
        int error,
        string operation)
    {
        if (error == 0)
            return;

        throw new OpenClException(
            operation,
            error);
    }
}


internal sealed class OpenClException : Exception
{
    public int ErrorCode { get; }

    public OpenClException(
        string operation,
        int errorCode,
        string? details = null)
        : base(CreateMessage(operation, errorCode, details))
    {
        ErrorCode = errorCode;
    }

    private static string CreateMessage(
        string operation,
        int errorCode,
        string? details)
    {
        string message =
            $"{operation} failed with OpenCL error " +
            $"{(ErrorCodes)errorCode} ({errorCode}).";

        if (!string.IsNullOrWhiteSpace(details))
        {
            message +=
                Environment.NewLine +
                Environment.NewLine +
                details;
        }

        return message;
    }
}
