namespace PixelFlut.Core
{
    public interface IPixelFlutInput
    {
        public event EventHandler? ChangeOfDevicesDetected;
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsNorthButtonPressed { get; set; }
        public bool IsEastButtonPressed { get; set; }
        public bool IsSouthButtonPressed { get; set; }
        public bool IsWestButtonPressed { get; set; }
        public bool IsStartButtonPressed { get; set; }
        public bool IsSelectButtonPressed { get; set; }
    }
}
