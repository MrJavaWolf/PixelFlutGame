using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureTakingDamage
{
    public float TakingDamageDuration = 0.6f;
    public float Gravity = 20f;

    private double StartTakingDamageTime = -1f;
    private Vector2 takingDamageForce = Vector2.Zero;

    private StickFigureBase stickFigureBase;

    public StickFigureTakingDamage(StickFigureBase stickFigureBase)
    {
        this.stickFigureBase = stickFigureBase;
    }

    public void StartTakeDamage(GameTime time, Vector2 force)
    {
        StartTakingDamageTime = time.TotalTime.TotalSeconds;
        takingDamageForce = force;
        stickFigureBase.Velocity = takingDamageForce;
        stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.TakeDamage);
    }

    public void Interrupt()
    {
        StartTakingDamageTime = -1;
    }

    public bool IsTakingDamage(GameTime time) => time.TotalTime.TotalSeconds - StartTakingDamageTime <= TakingDamageDuration;

    // Update is called once per frame
    public void Loop(GameTime time)
    {
        stickFigureBase.Velocity =
            stickFigureBase.Velocity + Vector2.UnitY * -1 * Gravity * (float)time.DeltaTime.TotalSeconds;
    }
}
