using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using PixelFlut.Core.Sprite;
using System.Numerics;

namespace StickFigureGame;

public class StickFigureExplosionEffectAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string projectileSheet = BasePath + "Effect/Explosion 1/explosion.png";

    private SpriteAnimation animation;
    private int pixelsPerUnit = 100;
    private int spriteWidth = 341;
    private int spriteHeight = 341;

    public StickFigureExplosionEffectAnimator(SpriteLoader spriteLoader)
    {
        animation = spriteLoader.LoadAnimation(projectileSheet, spriteWidth, spriteHeight, pixelsPerUnit, TimeSpan.FromMilliseconds(33),
            loopAnimation: false);
    }

    public bool IsAnimationDone(GameTime time) => animation.IsAnimationDone(time);

    public void Play(Vector2 centerPosition, GameTime time)
    {

        // The animation wants lower left conor, so we calculate the center to lower left offset
        Vector2 offset = new Vector2(spriteWidth / 2.0f / pixelsPerUnit, spriteHeight / 2.0f / pixelsPerUnit);
        animation.SetPosition(centerPosition - offset);
        animation.Restart(time);
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        return animation.Render(time);
    }
}



/// <summary>
/// Allow the <see cref="StickFigureExplosionEffectAnimator"/> to be pooled so we do not have to load the image from the disk more times than nessesary 
/// </summary>
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
