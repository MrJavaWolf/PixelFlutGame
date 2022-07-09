namespace PixelFlut.Core
{
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
        public float DeadzoneRadius { get; set; }
    }
}
