namespace PixelFlut.Core;

public interface IPixelFlutScreenSocket
{
     void Render(List<PixelBuffer> frame, PixelFlutScreenStats stats);
}
