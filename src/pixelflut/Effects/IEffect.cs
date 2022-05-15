using PixelFlut.Core;

namespace PixelFlut.Effect;

public interface IEffect
{
    public bool IsAlive { get; }
    public PixelBuffer PixelBuffer { get; }
    public void Loop(GameTime time);
    public void Renderer();
}
