using Humper;
using Humper.Responses;
using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureBase
{
    public enum FacingDirection { Left, Right };

    public StickFigureAnimator PlayerAnimator { get; private set; }

    public float MovementSpeed { get; set; } = 10;

    public Vector2 Velocity;

    public FacingDirection Facing { get; set; } = FacingDirection.Right;

    public bool IsGrounded = true;

    public Vector2 Position => box == null ? Vector2.Zero : new Vector2(box.X, box.Y);

    public Vector2 Size { get; set; } = new Vector2(0.65f, 1.25f);

    private StickFigureWorld world;

    public IBox box { get; private set; }

    public StickFigureBase(
        StickFigureWorld world, 
        Vector2 spawnLocation, 
        SpriteLoader spriteLoader)
    {
        this.world = world;
        box = world.BoxWorld.Create(
            spawnLocation.X,
            spawnLocation.Y,
            Size.X,
            Size.Y);
        PlayerAnimator = new StickFigureAnimator(this, spriteLoader);
        PlayerAnimator.Play(StickFigureAnimation.Idle);
    }


    public void Teleport(Vector2 to)
    {
        Velocity = Vector2.UnitY;
        box.Move(to.X, to.Y, c => CollisionResponses.None);
    }

    public void Loop(GameTime time)
    {
        this.Move(time);
    }

    private void Move(GameTime time)
    {
        Vector2 toPosition = this.Position + this.Velocity * (float)time.DeltaTime.TotalSeconds;

        Vector2 prevPositin = new Vector2(box.X, box.Y);

        if (Velocity.Y > 0)
        {
            IsGrounded = false;
        }

        bool hitGround = false;
        box.Move(toPosition.X, toPosition.Y, c =>
        {
            if (c.Hit.Normal.X != 0)
            {
                this.Velocity = new Vector2(0, this.Velocity.Y);
            }
            if (c.Hit.Normal.Y != 0)
            {
                this.Velocity = new Vector2(this.Velocity.X, 0);
            }

            if (c.Hit.Normal.Y > 0)
            {
                hitGround = true;
            }
            return CollisionResponses.Slide;
        });

        IsGrounded = hitGround;
    }
}
