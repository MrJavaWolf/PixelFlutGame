namespace PixelFlut.Core
{
    public interface IPixelFlutInput
    {

        /// <summary>
        /// Is triggered every time a devices have been plugged in or unplugged
        /// </summary>
        public event EventHandler? ChangeOfDevicesDetected;

        /// <summary>
        /// The horizontal amount, range 0 - 1 where
        /// 0   = All left
        /// 0.5 = Middel
        /// 1   = All Right
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// The vertical amount, range 0 - 1 where
        /// 0   = All up
        /// 0.5 = Middel
        /// 1   = All down
        /// </summary>
        public double Y { get; set; }
        public bool IsNorthButtonPressed { get; set; }
        public bool IsEastButtonPressed { get; set; }
        public bool IsSouthButtonPressed { get; set; }
        public bool IsWestButtonPressed { get; set; }

        /// <summary>
        /// Whether the start button is pressed
        /// </summary>
        public bool IsStartButtonPressed { get; set; }

        /// <summary>
        /// Whether the select button is pressed
        /// </summary>
        public bool IsSelectButtonPressed { get; set; }
    }
}
