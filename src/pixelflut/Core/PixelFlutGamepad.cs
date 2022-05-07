using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Usages;
using HidSharp;

namespace PixelFlut.Core;

public class PixelFlutGamepadConfiguration
{

    /// <summary>
    /// How often will we scan for device changes
    /// </summary>
    public TimeSpan DeviceScanFrequency { get; set; }

    /// <summary>
    /// How big is the deadzone of the X/Y values (0 = left/up, 0.5 = middle, 1=right/down)
    /// Used for analog sticks
    /// </summary>
    public float DeadzoneSize { get; set; }
}

public class PixelFlutGamepad : IPixelFlutInput
{
    private static class ButtonId
    {
        public const int Gamepad_X = 589825;
        public const int Gamepad_Y = 589828;
        public const int Gamepad_A = 589826;
        public const int Gamepad_B = 589827;
        public const int Gamepad_Horizontal = 65584;
        public const int Gamepad_Vertical = 65585;
        public const int Gamepad_Start = 589834;
        public const int Gamepad_Select = 589833;

        public const int PSController_X = 589826;
        public const int PSController_Circle = 589827;
        public const int PSController_Squre = 589825;
        public const int PSController_Triangle = 589828;
        public const int PSController_Horizontal = 65584;
        public const int PSController_Vertical = 65585;
        public const int PSController_Start = 589834;
        public const int PSController_Select = 589833;
    }


    public event EventHandler? ChangeOfDevicesDetected;
    public double X { get; set; } = 0.5;
    public double Y { get; set; } = 0.5;
    public bool IsNorthButtonPressed { get; set; } = false;
    public bool IsEastButtonPressed { get; set; } = false;
    public bool IsSouthButtonPressed { get; set; } = false;
    public bool IsWestButtonPressed { get; set; } = false;
    public bool IsStartButtonPressed { get; set; } = false;
    public bool IsSelectButtonPressed { get; set; } = false;

    private readonly PixelFlutGamepadConfiguration configuration;
    private ILogger<PixelFlutGamepad> logger;
    private ILogger<Devices> devicesLogger;

    public PixelFlutGamepad(
        PixelFlutGamepadConfiguration configuration,
        ILogger<PixelFlutGamepad> logger,
        ILogger<Devices> devicesLogger)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.devicesLogger = devicesLogger;
        logger.LogInformation($"Gamepad: {{@configuration}}", configuration);
    }

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await ListenForGamepadInputsAsync(token);
        }
    }

    private async Task ListenForGamepadInputsAsync(CancellationToken token)
    {

        // Subscribes to all inputs from all devices 
        // Remark:
        // On Windows devices.Connected(...)  will automatically detect new devices, but on Linux it will not.
        // Due to this we will re-subscribe to all inputs everytime a change of connected devices of been detected (on all platforms)

        using var devices = new Devices(devicesLogger);
        using var subscription =
            devices.Connected(
            device => device.ControlUsagesAll(GenericDesktopPage.X, GenericDesktopPage.Y))
            .Subscribe(devices =>
            {
                foreach (var device in devices)
                {
                    device.Current.Subscribe(changes =>
                    {
                        foreach (ControlChange change in changes)
                        {
                            foreach (Usage usage in change.Control.Usages)
                            {
                                if (usage.Types.Count() == 0) continue;
                                HandleGamepadInput(device.Current, change, usage);
                            }
                        }
                    });
                }
            });
        await WaitForDeviceChangeAsync(token);
    }

    /// <summary>
    /// Handle game inputs
    /// </summary>
    /// <param name="device"></param>
    /// <param name="change"></param>
    /// <param name="usage"></param>
    private void HandleGamepadInput(DevDecoder.HIDDevices.Device device, ControlChange change, Usage usage)
    {
        logger.LogInformation(
            "Gamepad input event detected: " +
            $"Device: {device.Name}" +
            $"Timestamp: {change.Timestamp}, " +
            $"Control: {change.Control}, " +
            $"Value: {change.Value}, " +
            $"Elapsed: {change.Elapsed}, " +
            $"");

        if (device.Name?.ToLower()?.Contains("sony") == true)
        {
            switch (usage.FullId)
            {
                case ButtonId.PSController_Circle:
                    this.IsEastButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.PSController_X:
                    this.IsSouthButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.PSController_Triangle:
                    this.IsNorthButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.PSController_Squre:
                    this.IsWestButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.PSController_Vertical:
                    this.Y = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case ButtonId.PSController_Horizontal:
                    this.X = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case ButtonId.PSController_Select:
                    this.IsSelectButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.PSController_Start:
                    this.IsStartButtonPressed = IsPressed(change.Value);
                    break;
            }
        }
        else
        {
            switch (usage.FullId)
            {
                case ButtonId.Gamepad_A:
                    this.IsEastButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.Gamepad_B:
                    this.IsSouthButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.Gamepad_X:
                    this.IsNorthButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.Gamepad_Y:
                    this.IsWestButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.Gamepad_Vertical:
                    this.Y = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case ButtonId.Gamepad_Horizontal:
                    this.X = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case ButtonId.Gamepad_Select:
                    this.IsSelectButtonPressed = IsPressed(change.Value);
                    break;
                case ButtonId.Gamepad_Start:
                    this.IsStartButtonPressed = IsPressed(change.Value);
                    break;
            }
        }
    }

    private bool IsPressed(double value) =>
        value > 0.5;

    private bool IsInDeadzone(double value) =>
        value > 0.5 - configuration.DeadzoneSize &&
        value < 0.5 + configuration.DeadzoneSize;

    /// <summary>
    /// This is a blocking call and will only return when a change of connected hid devices have been detected.
    /// </summary>
    private async Task WaitForDeviceChangeAsync(CancellationToken token)
    {
        var originalHidDevices = DeviceList.Local.GetHidDevices();
        PrintDevices(originalHidDevices);
        bool changeDetected = false;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(configuration.DeviceScanFrequency, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            var currentHidDevices = DeviceList.Local.GetHidDevices();
            if (originalHidDevices.Count() != currentHidDevices.Count())
            {
                // A different number of hid devices are connected means a hid device have been either connected or disconnected
                changeDetected = true;
                break;
            }
            for (int i = 0; i < currentHidDevices.Count(); i++)
            {
                HidDevice originalDevice = originalHidDevices.ElementAt(i);
                HidDevice currentDevice = currentHidDevices.ElementAt(i);
                if (originalDevice.DevicePath != currentDevice.DevicePath ||
                    originalDevice.VendorID != currentDevice.VendorID ||
                    originalDevice.ProductID != currentDevice.ProductID ||
                    originalDevice.ReleaseNumber != currentDevice.ReleaseNumber ||
                    originalDevice.ReleaseNumberBcd != currentDevice.ReleaseNumberBcd)
                {
                    // The current device is not the same of the orginal device, something have changed
                    // May occour if the user disconnectes a device and connects a different device before a scan was made
                    changeDetected = true;
                    break;
                }
            }
            if (changeDetected) break;
        }
        if (changeDetected)
        {
            logger.LogInformation($"Detected a change of connected devices...");
            ChangeOfDevicesDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Prints a list of devices
    /// </summary>
    private void PrintDevices(IEnumerable<HidDevice> hidDevices)
    {
        int i = 0;
        logger.LogInformation($"Number of HID devices: {hidDevices.Count()}");
        foreach (var hidDevice in hidDevices)
        {
            i++;
            string friendlyName = "<Unknown name>";
            try
            {
                friendlyName = hidDevice.GetFriendlyName();
            }
            catch { }
            logger.LogInformation($"{i}: " +
                $"{friendlyName}, " +
                $"{hidDevice.DevicePath}, " +
                $"{hidDevice.VendorID}, " +
                $"{hidDevice.ProductID}" +
                $"{hidDevice.ReleaseNumber}" +
                $"{hidDevice.ReleaseNumberBcd}");
        }
    }

}
