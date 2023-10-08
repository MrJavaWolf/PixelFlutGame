using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using System.Numerics;

namespace StickFigureGame;

public class StickFigureExplosionEffect
{
    private readonly StickFigureWorld world;
    private readonly ObjectPool<StickFigureExplosionEffectAnimator> explosionAnimators;
    public StickFigureExplosionEffectAnimator Animator { get; }

    public StickFigureExplosionEffect(
        StickFigureWorld world,
        ObjectPool<StickFigureExplosionEffectAnimator> explosionAnimators)
    {
        this.world = world;
        this.explosionAnimators = explosionAnimators;
        world.Explosions.Add(this);
        Animator = explosionAnimators.Get();
    }

    public void Play(Vector2 position, GameTime time)
    {
        Animator.Play(position, time);
    }

    public void Loop(GameTime time)
    {
        if (Animator.IsAnimationDone(time))
        {
            world.Explosions.Remove(this);
            explosionAnimators.Return(Animator);
        }
    }
}
