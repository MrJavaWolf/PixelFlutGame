using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Numerics;

namespace StickFigureGame;

public class StickFigureExplosionEffectAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string projectileSheet = BasePath + "Effect/Explosion 1/explosion.png";
    private readonly SpriteLoader spriteLoader;

    private SpriteAnimation explosion;

    public StickFigureExplosionEffectAnimator(SpriteLoader spriteLoader)
    {
        this.spriteLoader = spriteLoader;
        explosion = spriteLoader.LoadAnimation(projectileSheet, 341, 341, TimeSpan.FromMilliseconds(25),
            loopAnimation: false);
    }

    public bool IsAnimationDone(GameTime time) => explosion.IsAnimationDone(time);

    public void Play(Vector2 position, GameTime time)
    {
        explosion.SetPosition(position);
        explosion.Restart(time);
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        return explosion.Render(time);
    }
}



public class StickFigureExplosionEffectAnimatorPooledObjectPolicy : IPooledObjectPolicy<StickFigureExplosionEffectAnimator>
{
    private readonly SpriteLoader spriteLoader;

    public StickFigureExplosionEffectAnimatorPooledObjectPolicy(SpriteLoader spriteLoader)
    {
        this.spriteLoader = spriteLoader;
    }

    public StickFigureExplosionEffectAnimator Create()
    {
        return new StickFigureExplosionEffectAnimator(spriteLoader);
    }

    public bool Return(StickFigureExplosionEffectAnimator obj)
    {
        return true;
    }
}
