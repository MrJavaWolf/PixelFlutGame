using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelFlut.Core;

public class PixelFlutScreenProtocol_JanKlopperPixelvloedC_P0 : IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; } = 140;
    public int BufferSize { get => HeaderSize + PixelsPerBuffer * BytesPerPixel; }
    public const int BytesPerPixel = 7;
    public const int HeaderSize = 2;

    public Memory<byte> CreateBuffer()
    {
        Memory<byte> send_buffer = new byte[HeaderSize + PixelsPerBuffer * BytesPerPixel];
        send_buffer.Span[0] = 0x00; // Protocol 0
        send_buffer.Span[1] = 0x00; // Not used
        return send_buffer;
    }

    public Memory<byte> WriteToBuffer(Memory<byte> send_buffer, int pixelNumber, int x, int y)
    {
        int offset = HeaderSize + pixelNumber * BytesPerPixel;
        Memory<byte> xBytes = BitConverter.GetBytes(x);
        Memory<byte> yBytes = BitConverter.GetBytes(y);
        send_buffer.Span[offset + 0] = xBytes.Span[0];
        send_buffer.Span[offset + 1] = xBytes.Span[1];
        send_buffer.Span[offset + 2] = yBytes.Span[0];
        send_buffer.Span[offset + 3] = yBytes.Span[1];
        return send_buffer;
    }

    public Memory<byte> WriteToBuffer(Memory<byte> send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a)
    {
        int offset = HeaderSize + pixelNumber * BytesPerPixel;
        Memory<byte> xBytes = BitConverter.GetBytes(x);
        Memory<byte> yBytes = BitConverter.GetBytes(y);
        send_buffer.Span[offset + 0] = xBytes.Span[0];
        send_buffer.Span[offset + 1] = xBytes.Span[1];
        send_buffer.Span[offset + 2] = yBytes.Span[0];
        send_buffer.Span[offset + 3] = yBytes.Span[1];
        send_buffer.Span[offset + 4] = r;
        send_buffer.Span[offset + 5] = g;
        send_buffer.Span[offset + 6] = b;
        return send_buffer;
    }

    public void Draw(Memory<byte> buffer, Image<Rgba32> toImage, int? numberOfPixels = null)
    {
        int pixels = numberOfPixels ?? PixelsPerBuffer;

        for (int i = 0; i < pixels; i++)
        {
            int offset = HeaderSize + i * BytesPerPixel;
            short x = BitConverter.ToInt16(buffer.Span[(offset + 0)..(offset + 1)]);
            short y = BitConverter.ToInt16(buffer.Span[(offset + 2)..(offset + 3)]);
            byte r = buffer.Span[(offset + 4)];
            byte g = buffer.Span[(offset + 5)];
            byte b = buffer.Span[(offset + 6)];
            if (x < 0 || x >= toImage.Width || y < 0 || y >= toImage.Height)
                continue;
            toImage[x, y] = new Rgba32(r, g, b);
        }
    }
}
