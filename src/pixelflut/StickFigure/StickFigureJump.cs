using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureJump
{
    public Vector2 velocity;
    private StickFigureBase stickFigureBase;

    // Jumping Stats

    /// <summary>
    /// Maximum jump height
    /// </summary>
    public float jumpHeight = 2.1f;

    /// <summary>
    /// How long it takes to reach that height before coming back down
    /// </summary>
    public float timeToJumpApex = 0.3f;

    /// <summary>
    /// Gravity multiplier to apply when going up
    /// </summary>
    public float upwardMovementMultiplier = 1f;

    /// <summary>
    /// Gravity multiplier to apply when coming down
    /// </summary>
    public float downwardMovementMultiplier = 1f;

    /// <summary>
    /// How many times can you jump in the air?
    /// </summary>
    public int maxAirJumps = 1;

    //Options

    /// <summary>
    /// Should the character drop when you let go of jump?
    /// </summary>
    public bool variablejumpHeight = true;

    /// <summary>
    /// Gravity multiplier when you let go of jump
    /// </summary>
    public float jumpCutOff = 2.25f;

    /// <summary>
    /// The fastest speed the character can fall
    /// </summary>
    public float speedLimit = 20;

    /// <summary>
    /// How long should coyote time last?
    /// </summary>
    public float coyoteTime = 0.15f;

    /// <summary>
    /// How far from ground should we cache your jump?
    /// </summary>
    public float jumpBuffer = 0.15f;
    public float gravity = -50;


    // Calculations
    public float jumpSpeed;
    private float defaultGravityScale = 1;
    public float gravMultiplier;

    // Current State
    private bool desiredJump;
    private double jumpBufferCounter;
    private double coyoteTimeCounter = 0;
    private bool pressingJump;
    private bool currentlyJumping;
    public int currentAirJump = 0;

    public StickFigureJump(StickFigureBase stickFigureBase)
    {
        this.stickFigureBase = stickFigureBase;
    }

    public void Loop(GameTime time, IGamePadDevice gamePad)
    {

        if (stickFigureBase.IsGrounded)
            currentAirJump = 0;
        
        bool jumpInput = gamePad.SouthButton.IsPressed;

        if (!pressingJump && jumpInput)
        {
            desiredJump = true;
        }
        pressingJump = jumpInput;


        //Jump buffer allows us to queue up a jump, which will play when we next hit the ground
        if (jumpBuffer > 0)
        {
            // Instead of immediately turning off "desireJump", start counting up...
            // All the while, the DoAJump function will repeatedly be fired off
            if (desiredJump)
            {
                jumpBufferCounter += time.DeltaTime.TotalSeconds;

                if (jumpBufferCounter > jumpBuffer)
                {
                    //If time exceeds the jump buffer, turn off "desireJump"
                    desiredJump = false;
                    jumpBufferCounter = 0;
                }
            }
        }

        //If we're not on the ground and we're not currently jumping, that means we've stepped off the edge of a platform.
        //So, start the coyote time counter...
        if (!currentlyJumping && !stickFigureBase.IsGrounded)
        {
            coyoteTimeCounter += time.DeltaTime.TotalSeconds;
        }
        else
        {
            //Reset it when we touch the ground, or jump
            coyoteTimeCounter = 0;
        }

        //Get velocity from Kit's Rigidbody 
        velocity = stickFigureBase.Velocity;

        //Keep trying to do a jump, for as long as desiredJump is true
        if (desiredJump)
        {
            if (CanJump())
            {
                Jump();
                desiredJump = false;
                //Skip gravity calculations this frame, so currentlyJumping doesn't turn off
                //This makes sure you can't do the coyote time double jump bug
                return;
            }

            if (jumpBuffer == 0)
            {
                //If we don't have a jump buffer, then turn off desiredJump immediately after hitting jumping
                desiredJump = false;
            }
        }

        //We change the character's gravity based on her Y direction
        //If Kit is going up...
        if (stickFigureBase.Velocity.Y > 0.01f)
        {
            if (stickFigureBase.IsGrounded)
            {
                //Don't change it if Kit is stood on something (such as a moving platform)
                gravMultiplier = defaultGravityScale;
            }
            else
            {
                //If we're using variable jump height...)
                if (variablejumpHeight)
                {
                    //Apply upward multiplier if player is rising and holding jump
                    if (pressingJump && currentlyJumping)
                    {
                        gravMultiplier = upwardMovementMultiplier;
                    }
                    //But apply a special downward multiplier if the player lets go of jump
                    else
                    {
                        gravMultiplier = jumpCutOff;
                    }
                }
                else
                {
                    gravMultiplier = upwardMovementMultiplier;
                }
            }
        }

        //Else if going down...
        else if (stickFigureBase.Velocity.Y < -0.01f)
        {

            if (stickFigureBase.IsGrounded)
            //Don't change it if Kit is stood on something (such as a moving platform)
            {
                gravMultiplier = defaultGravityScale;
            }
            else
            {
                //Otherwise, apply the downward gravity multiplier as Kit comes back to Earth
                gravMultiplier = downwardMovementMultiplier;
            }
        }
        //Else not moving vertically at all
        else
        {
            if (stickFigureBase.IsGrounded)
            {
                currentlyJumping = false;
            }
            gravMultiplier = defaultGravityScale;
        }

        velocity = new Vector2(velocity.X,
            (float)(velocity.Y + gravity * gravMultiplier * time.DeltaTime.TotalSeconds));

        //Set the character's Rigidbody's velocity
        //But clamp the Y variable within the bounds of the speed limit, for the terminal velocity assist option
        stickFigureBase.Velocity = new Vector2(velocity.X, Math.Clamp(velocity.Y, -speedLimit, 100));
    }


    private bool CanJump()
     => stickFigureBase.IsGrounded ||
        (coyoteTimeCounter > 0.03f && coyoteTimeCounter < coyoteTime) ||
        currentAirJump < maxAirJumps;


    private void Jump()
    {
        //If we have double jump on, allow us to jump again (but only once)
        if (stickFigureBase.IsGrounded || (coyoteTimeCounter > 0.03f && coyoteTimeCounter < coyoteTime))
        {
            currentAirJump = 0;
        }
        else
        {
            currentAirJump = currentAirJump + 1;
        }

        desiredJump = false;
        jumpBufferCounter = 0;
        coyoteTimeCounter = 0;

        //Determine the power of the jump, based on our gravity and stats
        jumpSpeed = MathF.Sqrt(-2f * gravity * jumpHeight);

        //If Kit is moving up or down when she jumps (such as when doing a double jump), change the jumpSpeed;
        //This will ensure the jump is the exact same strength, no matter your velocity.
        if (velocity.Y > 0f)
        {
            jumpSpeed = Math.Max(jumpSpeed - velocity.Y, 0f);
        }
        else if (velocity.Y < 0f)
        {
            jumpSpeed += Math.Abs(stickFigureBase.Velocity.Y);
        }

        //Apply the new jumpSpeed to the velocity. It will be sent to the Rigidbody in FixedUpdate;
        velocity.Y += jumpSpeed;
        stickFigureBase.Velocity = velocity;
        currentlyJumping = true;
    }
}
