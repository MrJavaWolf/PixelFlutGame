using System.Numerics;

namespace PixelFlut.Pong;

public class PongGameState
{
    /// <summary>
    /// Ball Center position
    /// </summary>
    public Vector2 BallPosition { get; set; }

    /// <summary>
    /// Ball verlocity
    /// </summary>
    public Vector2 BallVerlocity { get; set; }

    /// <summary>
    /// Player 1's top- and left-most position
    /// </summary>
    public Vector2 Player1Position { get; set; }

    /// <summary>
    /// Player 1 last hit the ball
    /// </summary>
    public TimeSpan Player1LastHitTime { get; set; }

    /// <summary>
    /// Player 2's top- and left-most position
    /// </summary>
    public Vector2 Player2Position { get; set; }

    /// <summary>
    /// Player 2 last hit the ball
    /// </summary>
    public TimeSpan Player2LastHitTime { get; set; }

    /// <summary>
    /// Number of times a player have hit the ball
    /// The more times a player hits the ball the faster it goes
    /// </summary>
    public int BallBounces { get; set; } = 0;

    public PongGameStateType CurrentGameState { get; set; } = PongGameStateType.StartScreen;


    public int PreviousWinner { get; set; } = -1;

    /// <summary>
    /// How many times the player 1 have scored 
    /// </summary>
    public int Player1Score { get; set; } = 0;

    /// <summary>
    /// How many times the player 2 have scored 
    /// </summary>
    public int Player2Score { get; set; } = 0;
}
