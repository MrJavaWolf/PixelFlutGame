using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureDash
{
    public float DashSpeed = 10f;
    public float DashDuration = 0.2f;
    public float DashCooldown = 0.5f;

    private double StartDashTime = -1f;
    private Vector2 dashDirection = Vector2.Zero;

    private StickFigureBase stickFigureBase;

    public StickFigureDash(StickFigureBase stickFigureBase)
    {
        this.stickFigureBase = stickFigureBase;
    }

    public bool CanStartDash(GameTime time) => time.TotalTime.TotalSeconds - StartDashTime - DashDuration > DashCooldown;

    public void StartDash(GameTime time, IGamePadDevice gamePad)
    {
        
        StartDashTime = time.TotalTime.TotalSeconds;
        Vector2 input = gamePad.LeftStickInput;
        if (input != Vector2.Zero)
        {
            dashDirection = Vector2.Normalize(input);
        }
        else
        {
            dashDirection = Vector2.UnitX * (
                stickFigureBase.Facing == StickFigureBase.FacingDirection.Left ? -1 : 1);
        }
        stickFigureBase.PlayerAnimator.Play("dash");
    }

    public void Interrupt()
    {
        StartDashTime = -1;
    }
    public bool IsDashing(GameTime time) => time.TotalTime.TotalSeconds - StartDashTime <= DashDuration;

    // Update is called once per frame
    public void Loop()
    {
        stickFigureBase.Velocity = dashDirection * DashSpeed;
    }
}
