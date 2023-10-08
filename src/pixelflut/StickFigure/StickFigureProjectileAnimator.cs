using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Numerics;

namespace StickFigureGame;
public class StickFigureProjectileAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string projectileSheet = BasePath + "Effect/Fireball 2/fireball.png";
    private readonly SpriteLoader spriteLoader;

    private SpriteAnimation projectile;

    public StickFigureProjectileAnimator(SpriteLoader spriteLoader)
    {
        this.spriteLoader = spriteLoader;
        projectile = spriteLoader.LoadAnimation(projectileSheet, 189, 189, TimeSpan.FromMilliseconds(25),
            new List<int>() { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 },
            loopAnimation: true);
    }

    public void Play(float angle, GameTime time)
    {
        projectile.Restart(time);
    }

    public void UpdatePosition(Vector2 position)
    {
        projectile.SetPosition(position);
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        return projectile.Render(time);
    }
}

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
