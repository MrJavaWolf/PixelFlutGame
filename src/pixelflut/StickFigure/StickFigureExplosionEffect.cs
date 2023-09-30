using System.Numerics;

namespace StickFigureGame;

public class StickFigureExplosionEffect
{
    private readonly StickFigureWorld world;

    public StickFigureExplosionEffect(StickFigureWorld world)
    {
        this.world = world;
        world.Explosions.Add(this);
    }

    public void Play(Vector2 position)
    {

    }
}
