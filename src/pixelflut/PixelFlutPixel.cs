namespace pixelflut
{
    public record PixelFlutPixel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public byte R { get; set; } = 255;
        public byte G { get; set; } = 255;
        public byte B { get; set; } = 255;
        public byte A { get; set; } = 255;
    }
}
