namespace PixelFlut.Core;

public interface IPixelFlutScreenProtocol
{
    public int PixelsPerBuffer { get; }
    public int BufferSize { get; }
    public byte[] CreateBuffer();
    public void WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a);
    public void WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y);
}
