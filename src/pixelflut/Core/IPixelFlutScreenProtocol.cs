using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelFlut.Core;

public interface IPixelFlutScreenProtocol
{

    public int PixelsPerBuffer { get; }
    public int BufferSize { get; }
    public byte[] CreateBuffer();
    public byte[] WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a);
    public byte[] WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y);
    public void Draw(byte[] buffer, Image<Rgba32> toImage, int? numberOfPixels = null);

}
