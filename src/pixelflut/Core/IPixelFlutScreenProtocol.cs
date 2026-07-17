using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelFlut.Core;

public interface IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; }
    public int BufferSize { get; }
    public Memory<byte> CreateBuffer();
    public Memory<byte> WriteToBuffer(Memory<byte> send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a);
    public Memory<byte> WriteToBuffer(Memory<byte> send_buffer, int pixelNumber, int x, int y);
    public void Draw(Memory<byte> buffer, Image<Rgba32> toImage, int? numberOfPixels = null);

}
