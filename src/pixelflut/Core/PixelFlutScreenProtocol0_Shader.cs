using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelFlut.Core;

public class PixelFlutScreenProtocol0_Shader : IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; } = 160;
    public int BufferSize { get => HeaderSize + PixelsPerBuffer * BytesPerPixel; }
    public const int BytesPerPixel = 7;
    public const int HeaderSize = 2;
    private readonly int width;
    private readonly int height;
    public byte[] FullBuffer { get; }
    private int numberOfBuffers;

    public PixelFlutScreenProtocol0_Shader(int width, int height)
    {
        this.width = width;
        this.height = height;
        int numberOfPixels = width * height;
        numberOfBuffers = numberOfPixels / PixelsPerBuffer;
        if (numberOfPixels % PixelsPerBuffer != 0)
        {
            numberOfBuffers += 1;
        }
        int fullBufferSize = numberOfBuffers * PixelsPerBuffer;
        FullBuffer = new byte[fullBufferSize];
    }

    public Memory<byte> CreateBuffer()
    {
        throw new NotImplementedException($"{nameof(PixelFlutScreenProtocol0_Shader)} does not support create buffer");
    }

    public Memory<byte> WriteToBuffer(Memory<byte> send_buffer, int pixelNumber, int x, int y)
    {
        throw new NotImplementedException($"{nameof(PixelFlutScreenProtocol0_Shader)} does not support create buffer");
    }

    public Memory<byte> WriteToBuffer(Memory<byte> send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a)
    {
        throw new NotImplementedException($"{nameof(PixelFlutScreenProtocol0_Shader)} does not support create buffer");
    }

    public void Draw(Memory<byte> buffer, Image<Rgba32> toImage, int? numberOfPixels = null)
    {
        int pixels = numberOfPixels ?? PixelsPerBuffer;

        for (int i = 0; i < pixels; i++)
        {
            int offset = HeaderSize + i * BytesPerPixel;
            short x = BitConverter.ToInt16(buffer.Span[(offset + 0)..(offset + 1 + 1)]);
            short y = BitConverter.ToInt16(buffer.Span[(offset + 2)..(offset + 3 + 1)]);
            byte r = buffer.Span[(offset + 4)];
            byte g = buffer.Span[(offset + 5)];
            byte b = buffer.Span[(offset + 6)];
            if (x < 0 || x >= toImage.Width || y < 0 || y >= toImage.Height)
                continue;
            toImage[x, y] = new Rgba32(r, g, b);
        }
    }
}
