namespace PixelFlut.Core
{
    public record PixelFlutPixel
    {
        /// <summary>
        /// The X position of the pixel
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// The Y position of the pixel
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// The red value of pixel
        /// </summary>
        public byte R { get; set; } = 0;

        /// <summary>
        /// The green value of pixel
        /// </summary>
        public byte G { get; set; } = 0;

        /// <summary>
        /// The blue value of pixel
        /// </summary>
        public byte B { get; set; } = 0;

        /// <summary>
        /// The alfa value of pixel
        /// </summary>
        public byte A { get; set; } = 255;
    }
}
