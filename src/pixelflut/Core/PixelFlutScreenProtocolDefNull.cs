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

    public void WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y)
    {
        throw new NotSupportedException($"Not supported");
        if (pixelNumber != 0)
            throw new Exception($"The {nameof(PixelFlutScreenProtocolDefNull)} can only have 1 pixel per byte array");

        string message = $"PX {x} {y}\n";
        byte[] bytes = UTF8Encoding.UTF8.GetBytes(message);
        if (send_buffer.Length < bytes.Length)
        {
            throw new Exception($"Failed to write to buffer: '{message}'");
        }
        Array.Copy(bytes, send_buffer, bytes.Length);
    }

    public void WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a)
    {
        if (pixelNumber != 0)
            throw new Exception($"The {nameof(PixelFlutScreenProtocolDefNull)} can only have 1 pixel per byte array");


        string message = $"PX {x} {y} {ToHex(r)}{ToHex(g)}{ToHex(b)}\n";
        byte[] bytes = UTF8Encoding.UTF8.GetBytes(message);
        if (send_buffer.Length != bytes.Length)
        {
            //throw new Exception($"Failed to write to buffer: '{message}'");
            Array.Resize(ref send_buffer, bytes.Length);

        }
        Array.Copy(bytes, send_buffer, bytes.Length);
    }

    private static string ToHex(byte b)
    {
        return BitConverter.ToString(new byte[] { b }).ToLower();
    }
}
