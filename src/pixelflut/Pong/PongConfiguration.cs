namespace PixelFlut.Pong;

public class PongConfiguration
{
    /// <summary>
    /// How big is the ball in pixel
    /// </summary>
    public int BallRadius { get; set; }

    /// <summary>
    /// How big is the border of the ball
    /// Used for rendering only this helps better see where the ball is
    /// </summary>
    public int BallBorder { get; set; }

    /// <summary>
    /// How fast the ball initially moves
    /// </summary>
    public double BallStartSpeed { get; set; }

    /// <summary>
    /// How much the balls speed will increase everytime a player hits the ball
    /// </summary>
    public double BallSpeedIncrease { get; set; }

    /// <summary>
    /// How tall is the player in pixels
    /// </summary>
    public int PlayerHeight { get; set; }

    /// <summary>
    /// How wide is the player in pixels
    /// </summary>
    public int PlayerWidth { get; set; }

    /// <summary>
    /// How fast can the player move [pixels/per second]
    /// </summary>
    public int PlayerSpeed { get; set; }

    /// <summary>
    /// How big is the border of the player
    /// Used for rendering only this helps better see where the player is
    /// </summary>
    public int PlayerBorder { get; set; }

    /// <summary>
    /// How far in from the edges are the player
    /// </summary>
    public int PlayerDistanceToSides { get; set; }

    /// <summary>
    /// When hitting the ball on the side of the player paddle, how steep an angle (in radians) is allowed.
    /// Lowering the value will make the ball go more at an angle
    /// Recommended range:
    /// - Minimum: 0.20 (~11.5 degrees)
    /// - Maximum: 0.75 (~45 degrees)
    /// </summary>
    public float PlayerMaxRebounceAngle { get; set; }

    /// <summary>
    /// How many times does a player need to score to win (Not implemented yet)
    /// </summary>
    public int NumberOfGoalsToWin { get; set; }
}
