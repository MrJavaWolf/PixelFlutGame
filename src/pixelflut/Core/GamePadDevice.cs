using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Usages;

namespace PixelFlut.Core
{
    public class GamePadDevice : IGamePadDevice
    {
        private readonly Device device;
        private readonly PixelFlutGamepadConfiguration configuration;
        private readonly ILogger<GamePadDevice> logger;
        private bool isStartButtonCurrentlyPressedDown = false;
        private bool isSelectButtonCurrentlyPressedDown = false;
        private bool isNorthButtonCurrentlyPressedDown = false;
        private bool isEastButtonCurrentlyPressedDown = false;
        private bool isSouthButtonCurrentlyPressedDown = false;
        private bool isWestButtonCurrentlyPressedDown = false;
        
        public double X { get; private set; } = 0.5;
        public double Y { get; private set; } = 0.5;

        public GamepadButton StartButton { get; private set; } = new GamepadButton();
        public GamepadButton SelectButton { get; private set; } = new GamepadButton();
        public GamepadButton NorthButton { get; private set; } = new GamepadButton();
        public GamepadButton EastButton { get; private set; } = new GamepadButton();
        public GamepadButton SouthButton { get; private set; } = new GamepadButton();
        public GamepadButton WestButton { get; private set; } = new GamepadButton();

        public GamePadDevice(Device device, PixelFlutGamepadConfiguration configuration, ILogger<GamePadDevice> logger)
        {
            this.device = device;
            this.configuration = configuration;
            this.logger = logger;
            device.Subscribe(changes =>
            {
                foreach (ControlChange change in changes)
                {
                    foreach (Usage usage in change.Control.Usages)
                    {
                        if (usage.Types.Count() == 0) continue;
                        HandleGamepadInput(change, usage);
                    }
                }
            });
        }

        public void Update()
        {
            StartButton.Update(isStartButtonCurrentlyPressedDown);
            SelectButton.Update(isSelectButtonCurrentlyPressedDown);
            NorthButton.Update(isNorthButtonCurrentlyPressedDown);
            EastButton.Update(isEastButtonCurrentlyPressedDown);
            SouthButton.Update(isSouthButtonCurrentlyPressedDown);
            WestButton.Update(isWestButtonCurrentlyPressedDown);
        }

        /// <summary>
        /// Handle game inputs
        /// </summary>
        /// <param name="device"></param>
        /// <param name="change"></param>
        /// <param name="usage"></param>
        private void HandleGamepadInput(ControlChange change, Usage usage)
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
                SonyController(change, usage);
            }
            else
            {
                NormalController(change, usage);
            }
        }

        private void NormalController(ControlChange change, Usage usage)
        {
            switch (usage.FullId)
            {
                case (uint)GenericDesktopPage.Y:
                    this.Y = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case (uint)GenericDesktopPage.X:
                    this.X = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case (uint)ButtonPage.Button0: // X
                    this.isNorthButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button1: // A
                    this.isEastButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button2: // B
                    this.isSouthButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button3: // Y
                    this.isWestButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button8: // Select
                    this.isSelectButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button9: // Start
                    this.isStartButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
            }
        }

        private void SonyController(ControlChange change, Usage usage)
        {
            // Handle PlayStation controller inputs
            // Due to nobody can agree, the sony controllers are slightly different from all other controllers
            switch (usage.FullId)
            {
                case (uint)GenericDesktopPage.Y:
                    this.Y = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case (uint)GenericDesktopPage.X:
                    this.X = IsInDeadzone(change.Value) ? 0.5 : change.Value;
                    break;
                case (uint)ButtonPage.Button0: // Square
                    this.isWestButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button1: // Circle
                    this.isSouthButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button2: // Cross
                    this.isEastButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button3: // Triangle
                    this.isNorthButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button8: // Select
                    this.isSelectButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
                case (uint)ButtonPage.Button9: // Start
                    this.isStartButtonCurrentlyPressedDown = IsPressed(change.Value);
                    break;
            }
        }

        private bool IsPressed(double value) =>
            value > 0.5;

        private bool IsInDeadzone(double value) =>
            value > 0.5 - configuration.DeadzoneRadius &&
            value < 0.5 + configuration.DeadzoneRadius;

    }
}
