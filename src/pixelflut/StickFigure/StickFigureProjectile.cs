using Humper;
using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureProjectile
{
    public Vector2 CenterPosition;

    public float radius = 0.5f;
    public float explosionRadius = 2.65f;
    public float pushbackDamage = 5f;
    public float speed = 3f;
    public float LifeTime = 2;

    private Vector2 direction;
    private StickFigureWorld world;
    private readonly ObjectPool<StickFigureProjectileAnimator> projectileAnimators;
    private readonly ObjectPool<StickFigureExplosionEffectAnimator> explosionAnimators;
    private StickFigureCharacterController? shotByPlayer;
    private double startTime = -1;

    public StickFigureProjectileAnimator Animator { get; }


    public StickFigureProjectile(
        StickFigureWorld world,
        ObjectPool<StickFigureProjectileAnimator> projectileAnimators,
        ObjectPool<StickFigureExplosionEffectAnimator> explosionAnimators)
    {
        this.world = world;
        this.projectileAnimators = projectileAnimators;
        this.explosionAnimators = explosionAnimators;
        Animator = projectileAnimators.Get();
    }

    public void DoStart(
        GameTime time,
        Vector2 direction,
        Vector2 startPosition,
        StickFigureCharacterController player)
    {
        this.CenterPosition = startPosition;
        startTime = time.TotalTime.TotalSeconds;
        this.shotByPlayer = player;
        this.direction = direction;
        if (!world.Projectiles.Contains(this))
        {
            world.Projectiles.Add(this);
        }

        // Angle for the fireball effect
        float angle = Vector2.UnitX.SignedAngle(direction);
        Animator.Play(-angle, time);
    }

    // Update is called once per frame
    public void Loop(GameTime time)
    {
        if (world == null) return;
        this.CenterPosition = new Vector2(
            (float)(CenterPosition.X + this.direction.X * speed * time.DeltaTime.TotalSeconds),
            (float)(CenterPosition.Y + this.direction.Y * speed * time.DeltaTime.TotalSeconds));
        Animator.UpdateCenterPosition(this.CenterPosition);
        if (time.TotalTime.TotalSeconds - startTime > LifeTime)
        {
            Explode(time);
            return;
        }
        else
        {
            // Hit a player
            foreach (var player in world.Players)
            {
                if (this.shotByPlayer == player) continue;
                if (IsCollisionDetected(player.Box))
                {
                    Explode(time);
                    return;
                }
            }

            // Hit a world box
            foreach (var worldBox in world.WorldBoxes)
            {
                if (IsCollisionDetected(worldBox))
                {
                    Explode(time);
                    return;
                }
            }

            // Hit another projectile
            for (int i = 0; i < world.Projectiles.Count; i++)
            {
                var projectile = world.Projectiles[i];
                if (projectile == this) continue;
                if (Vector2.Distance(CenterPosition, projectile.CenterPosition) <= radius * 2)
                {
                    Explode(time);
                    projectile.Explode(time);
                    return;
                }
            }
        }
    }

    public bool IsCollisionDetected(IBox box)
    {
        // Calculate the center points of the square and circle.
        Vector2 squareCenter = new Vector2(box.X + box.Width / 2, box.Y + box.Height / 2);
        Vector2 circleCenter = this.CenterPosition;

        // Calculate the distance between the square's center and the circle's center.
        float distanceX = Math.Abs(circleCenter.X - squareCenter.X);
        float distanceY = Math.Abs(circleCenter.Y - squareCenter.Y);

        // Calculate the closest point on the square to the circle.
        float closestX = Math.Clamp(distanceX, -box.Width / 2, box.Width / 2);
        float closestY = Math.Clamp(distanceY, -box.Height / 2, box.Height / 2);

        // Calculate the distance from the closest point on the square to the circle's center.
        float distanceToClosestPoint = Vector2.Distance(new Vector2(distanceX, distanceY), new Vector2(closestX, closestY));

        // Check if the distance to the closest point is less than the circle's radius.
        return distanceToClosestPoint <= radius / 2;
    }

    public void Explode(GameTime time)
    {
        if (world == null) return;
        world.Projectiles.Remove(this);
        foreach (var player in world.Players)
        {
            Vector2 projectileCenter = CenterPosition;
            Vector2 playerCenter = player.Center;
            if (Vector2.Distance(projectileCenter, playerCenter) < explosionRadius)
            {
                Vector2 damageDirection;
                if (playerCenter != projectileCenter)
                    damageDirection = Vector2.Normalize(playerCenter - projectileCenter);
                else
                    damageDirection = Vector2.UnitY;

                player.TakeDamage(damageDirection * pushbackDamage, time);
            }
        }

        StickFigureExplosionEffect explosionEffect = new(world, explosionAnimators);
        explosionEffect.Play(CenterPosition, time);
        world.Projectiles.Remove(this);
        projectileAnimators.Return(Animator);
    }
}
