using PixelFlut.Core;
using System;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureMovement
{
    ///<summary>
    /// Maximum movement speed
    ///</summary>
    public float maxSpeed = 6f;

    /// <summary>
    /// How fast to reach max speed
    /// </summary>
    public float maxAcceleration = 60f;

    /// <summary>
    /// How fast to stop after letting go
    /// </summary>
    public float maxDecceleration = 35f;

    /// <summary>
    /// How fast to stop when changing direction
    /// </summary>
    public float maxTurnSpeed = 60f;

    /// <summary>
    /// How fast to reach max speed when in mid-air
    /// </summary>
    public float maxAirAcceleration = 30;

    /// <summary>
    /// How fast to stop in mid-air when no direction is used
    /// </summary>
    public float maxAirDeceleration = 15;

    /// <summary>
    /// How fast to stop when changing direction when in mid-air
    /// </summary>
    public float maxAirTurnSpeed = 30f;

    /// <summary>
    /// Friction to apply against movement on stick
    /// </summary>
    private float friction = 0;

    /// <summary>
    /// When false, the charcter will skip acceleration and deceleration and instantly move and stop
    /// </summary>
    public bool useAcceleration = true;

    // Calculations
    public float directionX;
    public Vector2 desiredVelocity;
    private double maxSpeedChange;
    private float acceleration;
    private float deceleration;
    private float turnSpeed;


    private StickFigureBase stickFigureBase;


    public StickFigureMovement(StickFigureBase stickFigureBase)
    {
        this.stickFigureBase = stickFigureBase;
    }

    // Update is called once per frame
    public void Loop(GameTime time, IGamePadDevice gamePad)
    {
        Vector2 input = gamePad.LeftStickInput;
        directionX = input.X;
        desiredVelocity = new Vector2(directionX, 0f) * Math.Max(maxSpeed - friction, 0f);

        //Set our acceleration, deceleration, and turn speed stats, based on whether we're on the ground on in the air
        acceleration = stickFigureBase.IsGrounded ? maxAcceleration : maxAirAcceleration;
        deceleration = stickFigureBase.IsGrounded ? maxDecceleration : maxAirDeceleration;
        turnSpeed = stickFigureBase.IsGrounded ? maxTurnSpeed : maxAirTurnSpeed;

        if (input.X != 0)
        {
            //If the sign (i.e. positive or negative) of our input direction doesn't match our movement, it means we're turning around and so should use the turn speed stat.
            if (Math.Sign(directionX) != Math.Sign(stickFigureBase.Velocity.X))
            {
                maxSpeedChange = turnSpeed * time.DeltaTime.TotalSeconds;
            }
            else
            {
                //If they match, it means we're simply running along and so should use the acceleration stat
                maxSpeedChange = acceleration * time.DeltaTime.TotalSeconds;
            }
        }
        else
        {
            //And if we're not pressing a direction at all, use the deceleration stat
            maxSpeedChange = deceleration * time.DeltaTime.TotalSeconds;
        }

        
        //Move our velocity towards the desired velocity, at the rate of the number calculated above
        stickFigureBase.Velocity = new Vector2(
            MoveTowards(stickFigureBase.Velocity.X, desiredVelocity.X, (float)maxSpeedChange),
            stickFigureBase.Velocity.Y);
        if (input.X != 0)
        {
            stickFigureBase.Facing = input.X > 0 ?
                StickFigureBase.FacingDirection.Right :
                StickFigureBase.FacingDirection.Left;
        }

        if (stickFigureBase.IsGrounded)
        {
            if (Math.Abs(stickFigureBase.Velocity.X) > 0)
            {
                stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.Run);
            }
            else
            {
                stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.Idle);
            }
        }
        else
        {
            if(Math.Abs(stickFigureBase.Velocity.Y) < 5)
            {
                stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.JumpTop);
            }
            else if (stickFigureBase.Velocity.Y > 0)
            {
                stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.JumpUp);
            }
            else
            {
                stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.JumpDown);
            }
        }

        stickFigureBase.PlayerAnimator.FlipX = stickFigureBase.Facing == StickFigureBase.FacingDirection.Left;

    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }
        return current + MathF.Sign(target - current) * maxDelta;
    }
}
