// https://github.com/defnull/pixelflut
using System.Text;

namespace PixelFlut.Core;

public class PixelFlutScreenProtocolDefNull : IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; } = 1;
    public int BufferSize { get => 19; }

    public byte[] CreateBuffer()
    {
        string message = $"PX {0} {0} {ToHex(0)}{ToHex(0)}{ToHex(0)}\n";
        byte[] send_buffer = UTF8Encoding.UTF8.GetBytes(message);
        return send_buffer;
    }

    public byte[] WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y)
    {
        if (pixelNumber != 0)
            throw new Exception($"The {nameof(PixelFlutScreenProtocolDefNull)} can only have 1 pixel per byte array");

        string message = $"PX {x} {y}";
        byte[] bytes = UTF8Encoding.UTF8.GetBytes(message);

        byte[] newSendBuffer = new byte[bytes.Length + 8];
        Array.Copy(bytes, newSendBuffer, bytes.Length);
        Array.Copy(send_buffer, 0, newSendBuffer, bytes.Length, 8);

        return newSendBuffer;
    }

    public byte[] WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a)
    {
        if (pixelNumber != 0)
            throw new Exception($"The {nameof(PixelFlutScreenProtocolDefNull)} can only have 1 pixel per byte array");
        string message = $"PX {x} {y} {ToHex(r)}{ToHex(g)}{ToHex(b)}\n";
        byte[] bytes = UTF8Encoding.UTF8.GetBytes(message);
        return bytes;
    }

    private static string ToHex(byte b)
    {
        return BitConverter.ToString(new byte[] { b }).ToLower();
    }
}
