using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Usages;
using HidSharp;
namespace PixelFlut.Core;

public class GamePadsController
{
    public event EventHandler? ChangeOfDevicesDetected;

    private readonly PixelFlutGamepadConfiguration configuration;
    private readonly ConsoleAsGamePad consoleGamePad;
    private ILogger<GamePadsController> logger;
    private ILoggerFactory loggerFactory;
    private List<IGamePadDevice> activeGamePads = new List<IGamePadDevice>();
    private IReadOnlyList<IGamePadDevice> connectedDevices = new List<IGamePadDevice>();

    public IReadOnlyList<IGamePadDevice> GamePads { get => activeGamePads; }


    public GamePadsController(
        PixelFlutGamepadConfiguration configuration,
        ILogger<GamePadsController> logger,
        ILoggerFactory loggerFactory,
        ConsoleAsGamePad consoleGamePad)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.loggerFactory = loggerFactory;
        this.consoleGamePad = consoleGamePad;
        logger.LogInformation($"Gamepad: {{@configuration}}", configuration);
    }

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // Subscribes to all inputs from all devices 
            // Remark:
            // On Windows devices.Connected(...)  will automatically detect new devices, but on Linux it will not.
            // Due to this we will re-subscribe to all inputs everytime a change of connected devices of been detected (on all platforms)
            using var devices = new Devices(loggerFactory.CreateLogger<Devices>());
            List<IGamePadDevice> newDevices = new() { consoleGamePad };
            using var subscription =
                devices.Connected(
                device => device.ControlUsagesAll(GenericDesktopPage.X, GenericDesktopPage.Y))
                .Subscribe(devices =>
                {
                    foreach (var device in devices)
                    {
                        GamePadDevice gamepadDevice = new(device.Current, configuration, loggerFactory.CreateLogger<GamePadDevice>());
                        newDevices.Add(gamepadDevice);
                    }
                });

            connectedDevices = newDevices;
            activeGamePads = new List<IGamePadDevice>();
            await WaitForDeviceChangeAsync(token);
        }
    }

    public void Loop()
    {
        // Make a local reference in case the connectedDevices list changes while updating the devices
        IReadOnlyList<IGamePadDevice> devices = connectedDevices;
        foreach (var device in devices)
        {
            if (device is ConsoleAsGamePad consoleGamePad)
                consoleGamePad.Loop();

            if (device is GamePadDevice gamePadDevice)
                gamePadDevice.Loop();

            if (device.StartButton.OnPress)
            {
                if (!activeGamePads.Contains(device))
                {
                    activeGamePads.Add(device);
                }
            }
        }
    }

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
