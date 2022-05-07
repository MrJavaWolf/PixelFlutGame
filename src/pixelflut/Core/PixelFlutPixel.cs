namespace PixelFlut.Core;

public record PixelFlutPixel
{
    /// <summary>
    /// The X position of the pixel
    /// </summary>
    public double X;

    /// <summary>
    /// The Y position of the pixel
    /// </summary>
    public double Y;

    /// <summary>
    /// The red value of pixel
    /// </summary>
    public byte R = 0;

    /// <summary>
    /// The green value of pixel
    /// </summary>
    public byte G = 0;

    /// <summary>
    /// The blue value of pixel
    /// </summary>
    public byte B = 0;

    /// <summary>
    /// The alfa value of pixel
    /// </summary>
    public byte A = 255;
}
