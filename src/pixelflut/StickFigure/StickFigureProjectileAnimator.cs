using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using PixelFlut.Core.Sprite;
using System.Numerics;

namespace StickFigureGame;
public class StickFigureProjectileAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string projectileSheet = BasePath + "Effect/Fireball 2/fireball.png";

    private SpriteAnimation animation;
    private int pixelsPerUnit = 300;
    private int spriteWidth = 512;
    private int spriteHeight = 384;

    public StickFigureProjectileAnimator(SpriteLoader spriteLoader)
    {
        animation = spriteLoader.LoadAnimation(projectileSheet, 512, 384, pixelsPerUnit, TimeSpan.FromMilliseconds(25),
            new List<int>() {12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27 },
            loopAnimation: true);
    }

    public void Play(float angle, GameTime time)
    {
        animation.Restart(time);
        animation.SetRotation(angle);
    }

    public void UpdateCenterPosition(Vector2 position)
    {
        // The animation wants lower left conor, so we calculate the center to lower left offset
        Vector2 offset = new Vector2(spriteWidth / 2.0f / pixelsPerUnit, spriteHeight / 2.0f / pixelsPerUnit);
        animation.SetPosition(position - offset);
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        return animation.Render(time);
    }
}


/// <summary>
/// Allow the <see cref="StickFigureProjectileAnimator"/> to be pooled so we do not have to load the image from the disk more times than nessesary 
/// </summary>
public class StickFigureProjectileAnimatorPooledObjectPolicy : IPooledObjectPolicy<StickFigureProjectileAnimator>
{
    private readonly SpriteLoader spriteLoader;

    public StickFigureProjectileAnimatorPooledObjectPolicy(SpriteLoader spriteLoader)
    {
        this.spriteLoader = spriteLoader;
    }

    public StickFigureProjectileAnimator Create()
    {
        return new StickFigureProjectileAnimator(spriteLoader);
    }

    public bool Return(StickFigureProjectileAnimator obj)
    {
        return true;
    }
}
