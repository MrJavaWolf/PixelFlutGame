using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelFlut.Core;

public class PixelFlutScreenProtocol0 : IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; } = 160;
    public int BufferSize { get => HeaderSize + PixelsPerBuffer * BytesPerPixel; }
    public const int BytesPerPixel = 7;
    public const int HeaderSize = 2;

    public byte[] CreateBuffer()
    {
        byte[] send_buffer = new byte[HeaderSize + PixelsPerBuffer * BytesPerPixel];
        send_buffer[0] = 0x00; // Protocol 1
        send_buffer[1] = 0x01; // Not used
        return send_buffer;
    }

    public byte[] WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y)
    {
        int offset = HeaderSize + pixelNumber * BytesPerPixel;
        byte[] xBytes = BitConverter.GetBytes(x);
        byte[] yBytes = BitConverter.GetBytes(y);
        send_buffer[offset + 0] = xBytes[0];
        send_buffer[offset + 1] = xBytes[1];
        send_buffer[offset + 2] = yBytes[0];
        send_buffer[offset + 3] = yBytes[1];
        return send_buffer;
    }

    public byte[] WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a)
    {
        int offset = HeaderSize + pixelNumber * BytesPerPixel;
        byte[] xBytes = BitConverter.GetBytes(x);
        byte[] yBytes = BitConverter.GetBytes(y);
        send_buffer[offset + 0] = xBytes[0];
        send_buffer[offset + 1] = xBytes[1];
        send_buffer[offset + 2] = yBytes[0];
        send_buffer[offset + 3] = yBytes[1];
        send_buffer[offset + 4] = r;
        send_buffer[offset + 5] = g;
        send_buffer[offset + 6] = b;
        return send_buffer;
    }

    public void Draw(byte[] buffer, Image<Rgba32> toImage, int? numberOfPixels = null)
    {
        int pixels = numberOfPixels ?? PixelsPerBuffer;

        for (int i = 0; i < pixels; i++)
        {
            int offset = HeaderSize + i * BytesPerPixel;
            short x = BitConverter.ToInt16(buffer[(offset + 0)..(offset + 1 +1)]);
            short y = BitConverter.ToInt16(buffer[(offset + 2)..(offset + 3 + 1)]);
            byte r = buffer[(offset + 4)];
            byte g = buffer[(offset + 5)];
            byte b = buffer[(offset + 6)];
            if (x < 0 || x >= toImage.Width || y < 0 || y >= toImage.Height)
                continue;
            toImage[x, y] = new Rgba32(r, g, b);
        }
    }
}
