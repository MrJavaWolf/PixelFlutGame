using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace PixelFlut.Core;

public class PixelFlutScreenRendererConfiguration
{
    /// <summary>
    /// The IP of the Pixelflut
    /// </summary>
    public string Ip { get; set; } = null!;

    /// <summary>
    /// The Port of the Pixelflut
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Set the on screen offset if you want the game to not run from 0,0 (usually top left)
    /// </summary>
    public int OffsetX { get; set; }

    /// <summary>
    /// Set the on screen offset if you want the game to not run from 0,0 (usually top left)
    /// </summary>
    public int OffsetY { get; set; }

    /// <summary>
    /// The size of the game area
    /// </summary>
    public int ResultionX { get; set; }

    /// <summary>
    /// The size of the game area
    /// </summary>
    public int ResultionY { get; set; }

    /// <summary>
    /// Sleep time between frames, set to -1 to run 100% CPU
    /// </summary>
    public int SleepTimeBetweenFrames { get; set; }
}

public class PixelFlutScreenStats
{
    /// <summary>
    /// How many bytes is sent
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// How many pixels is sent
    /// </summary>
    public long PixelsSent { get; set; }

    /// <summary>
    /// How many buffers is sent
    /// </summary>
    public long BuffersSent { get; set; }

    /// <summary>
    /// How many buffers is sent
    /// </summary>
    public long TotalBuffersSent { get; set; }

    /// <summary>
    /// The number of times we recived a new list of pixelbuffers from the game loop
    /// </summary>
    public long FramesFromGameLoop { get; set; }

    /// <summary>
    /// The number of pixelbuffers
    /// </summary>
    public long PixelBuffersFromGameLoop { get; set; }

    /// <summary>
    /// How many pixels the gameloop have produced to be rendered
    /// </summary>
    public long PixelsFromGameLoop { get; set; }
}

public class PixelFlutScreenRenderer
{
    // Generel
    private readonly PixelFlutScreenRendererConfiguration configuration;
    private readonly ILogger<PixelFlutScreenRenderer> logger;

    // Connection
    private Socket socket;
    private IPEndPoint endPoint;

    // Stats
    private Stopwatch statsPrinterStopwatch = new();
    private PixelFlutScreenStats stats = new();

    // The buffers we are currently rendering
    private List<PixelBuffer> frame = new();
    private int currentRenderFrameBuffer = 0;
    private int currentRenderByteBuffer = 0;

    public PixelFlutScreenRenderer(
        PixelFlutScreenRendererConfiguration configuration,
        ILogger<PixelFlutScreenRenderer> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
        logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);

        // Setup connnection
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
        endPoint = new IPEndPoint(serverAddr, configuration.Port);

        // Stats counter
        statsPrinterStopwatch.Start();
    }

    public void SetFrame(List<PixelBuffer> frame)
    {
        this.frame = frame;
        stats.FramesFromGameLoop++;
        stats.PixelBuffersFromGameLoop += frame.Count;
        stats.PixelsFromGameLoop += frame.Sum(f => f.NumberOfPixels);
        PrintAndResetStats();
    }

    public void PrintAndResetStats()
    {
        if (statsPrinterStopwatch.ElapsedMilliseconds > 1000)
        {
            logger.LogInformation("Screen: {@stats}", stats);
            long totalBuffers = stats.TotalBuffersSent;
            stats = new PixelFlutScreenStats();
            stats.TotalBuffersSent += totalBuffers;
            statsPrinterStopwatch.Restart();
        }
    }

    public void Render()
    {
        if (frame.Count == 0) return;

        // Pick a buffer to render
        (int pixels, byte[] sendBuffer) = SelectNextBuffer();

        // Send 
        int bytesSent = socket.SendTo(sendBuffer, endPoint);

        // Update stats
        stats.BytesSent += bytesSent;
        stats.PixelsSent += pixels;
        stats.BuffersSent++;
        stats.TotalBuffersSent++;

        // Wait if requested, usefull on slower single core CPU's
        if (configuration.SleepTimeBetweenFrames != -1)
        {
            Thread.Sleep(configuration.SleepTimeBetweenFrames);
        }
    }

    private (int pixels, byte[] sendBuffer) SelectNextBuffer()
    {
        PixelBuffer buffer = frame[currentRenderFrameBuffer];
        byte[] sendBuffer = buffer.Buffers[currentRenderByteBuffer];
        int pixelsPerBuffer = buffer.PixelsPerBuffer;

        // Increment to select the next buffer
        currentRenderByteBuffer++;
        if (currentRenderByteBuffer >= buffer.Buffers.Count)
        {
            currentRenderByteBuffer = 0;
            currentRenderFrameBuffer++;
            if (currentRenderFrameBuffer >= frame.Count)
            {
                currentRenderFrameBuffer = 0;
            }
        }
        return (pixelsPerBuffer, sendBuffer);
    }
}


