namespace PixelFlut.Core;

public class PixelFlutScreenProtocol_JanKlopperPixelvloedC_P0 : IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; } = 140;
    public int BufferSize { get => HeaderSize + PixelsPerBuffer * BytesPerPixel; }
    public const int BytesPerPixel = 7;
    public const int HeaderSize = 2;

    public byte[] CreateBuffer()
    {
        byte[] send_buffer = new byte[HeaderSize + PixelsPerBuffer * BytesPerPixel];
        send_buffer[0] = 0x00; // Protocol 0
        send_buffer[1] = 0x00; // Not used
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
}
