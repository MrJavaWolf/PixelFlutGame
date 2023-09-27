using Humper;
using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureProjectile
{
    public Vector2 Position;

    public float radius = 0.5f;
    public float explosionRadius = 3f;
    public float pushbackDamage = 5f;
    public float speed = 20;
    public float LifeTime = 2;

    private Vector2 direction;
    private StickFigureWorld? world;
    private StickFigureCharacterController? shotByPlayer;
    private double startTime = -1;

    public GameObject ExplosionEffect;

    public void DoStart(
        GameTime time, 
        Vector2 direction, 
        Vector2 startPosition, 
        StickFigureWorld world, 
        StickFigureCharacterController player)
    {
        this.Position = startPosition;
        startTime = time.TotalTime.TotalSeconds;
        this.shotByPlayer = player;
        this.world = world;
        this.direction = direction;
        if (!world.Projectiles.Contains(this))
        {
            world.Projectiles.Add(this);
        }

        // Angle for the fireball effect
        float angle = Vector2.UnitX.SignedAngle(direction);
        transform.rotation = Quaternion.Euler(0, 0, angle);

    }

    // Update is called once per frame
    void Loop(GameTime time)
    {
        if (world == null) return;
        this.Position = new Vector2(
            (float)(Position.X + this.direction.X * speed * time.DeltaTime.TotalSeconds),
            (float)(Position.Y + this.direction.Y * speed * time.DeltaTime.TotalSeconds));
        if (time.DeltaTime.TotalSeconds - startTime > LifeTime)
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
                if (Vector2.Distance(Position, projectile.Position) <= radius * 2)
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
        Vector2 circleCenter = this.Position;

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
            Vector2 projectileCenter = Position;
            Vector2 playerCenter = player.Center;
            if (Vector2.Distance(projectileCenter, playerCenter) < explosionRadius)
            {
                Vector2 damageDirection = Vector2.Normalize(playerCenter - projectileCenter);
                player.TakeDamage(damageDirection * pushbackDamage, time);
            }
        }
        Instantiate(ExplosionEffect, Position, Quaternion.identity);
        Destroy(this.gameObject);
    }
}
