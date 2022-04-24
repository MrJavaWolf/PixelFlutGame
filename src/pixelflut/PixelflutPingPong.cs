namespace pixelflut
{
    public class PixelflutPingPong
    {
        private readonly PixelFlutGamepad gamepad;
        private List<PixelFlutPixel> pixels = new List<PixelFlutPixel>();


        public PixelflutPingPong(PixelFlutGamepad gamepad)
        {
            this.gamepad = gamepad;
        }

        public void Startup()
        {

        }

        public List<PixelFlutPixel> Loop()
        {
            return pixels;
        }
    }
}
