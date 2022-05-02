using Microsoft.Extensions.Logging;
using PixelFlut.Core;
using System.Numerics;
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

public enum PongGameStateType
{
    StartScreen,
    Playing,
    Score,
}

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
    /// Player 2's top- and left-most position
    /// </summary>
    public Vector2 Player2Position { get; set; }

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

public class PongGame
{
    private readonly PongConfiguration pongConfig;
    private readonly IPixelFlutInput input;
    private readonly PixelFlutScreenRendererConfiguration screenConfig;
    private readonly ILogger<PongGame> logger;
    private PongGameState gameState = new();
    private List<PixelFlutPixel> frame = new();
    public int MinimumYPlayerPosition { get => 0; }
    public int MaximumYPlayerPosition { get => screenConfig.ResultionY - pongConfig.PlayerHeight; }

    public PongGame(
        PongConfiguration pongConfig,
        IPixelFlutInput input,
        PixelFlutScreenRendererConfiguration screenConfig,
        ILogger<PongGame> logger,
        PongGameState? pongGameState = null)
    {
        this.pongConfig = pongConfig;
        this.input = input;
        this.screenConfig = screenConfig;
        this.logger = logger;
        if (pongGameState != null)
            gameState = pongGameState;
    }

    public void Startup()
    {
        logger.LogInformation("Initializes game: Pong");
        gameState = new();
        gameState.Player1Position = new(
            pongConfig.PlayerDistanceToSides,
            screenConfig.ResultionY / 2 - pongConfig.PlayerHeight / 2);
        gameState.Player2Position = new(
            screenConfig.ResultionX - pongConfig.PlayerDistanceToSides,
            screenConfig.ResultionY / 2 - pongConfig.PlayerHeight / 2);
        gameState.Player1Score = 0;
        gameState.Player2Score = 0;
        ResetBall();
    }

    private void ResetBall()
    {
        logger.LogInformation("Resets Pong ball");
        double startXYBallVerlocitySplit = Math.Min(0.7, Random.Shared.NextDouble());
        bool leftRight = Random.Shared.NextDouble() < 0.5;
        bool upDown = Random.Shared.NextDouble() < 0.5;

        //// Debug values
        //startXYBallVerlocitySplit = 0.09;
        //leftRight = true;
        //upDown = true;

        gameState.BallPosition = new Vector2(
            screenConfig.ResultionX / 2,
            screenConfig.ResultionY / 2);
        gameState.BallVerlocity = new Vector2(
            (float)((leftRight ? -1 : 1) * pongConfig.BallStartSpeed * (1 - startXYBallVerlocitySplit)),
            (float)((upDown ? -1 : 1) * pongConfig.BallStartSpeed * startXYBallVerlocitySplit));
        gameState.BallBounces = 0;
    }

    public (int numberOfPixels, List<PixelFlutPixel> frame) Loop(GameTime time)
    {
        // Update player position
        gameState.Player1Position = CalculateNewPlayerPosition(gameState.Player1Position, input.Y, time);
        gameState.Player2Position = CalculateNewPlayerPosition(gameState.Player2Position, GetPlayer2Input(), time);

        // Update ball
        UpdateBall(time);

        // Renderer
        int numberOfPixels = PongFrameRenderer.DrawFrame(pongConfig, gameState, frame);
        return (numberOfPixels, frame);
    }

    private double GetPlayer2Input()
    {
        // Currently player 2 input is the right side of the controller
        if (input.IsNorthButtonPressed && !input.IsSouthButtonPressed)
            return 0;
        else if (!input.IsNorthButtonPressed && input.IsSouthButtonPressed)
            return 1;
        else
            return 0.5;
    }

    private void UpdateBall(GameTime time)
    {
        Vector2 previousBallPosition = gameState.BallPosition;

        // Move the ball
        UpdateBallPosition(time);

        // Handle if the ball was hit by a player
        HandlePlayerHit(previousBallPosition);

        // Goal by player 1
        if (gameState.BallPosition.X > screenConfig.ResultionX)
        {
            gameState.Player1Score++;
            logger.LogInformation("GOAL - Player 1 scores");
            logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
            ResetBall();
        }

        // Goal by player 2
        else if (gameState.BallPosition.X < 0)
        {
            gameState.Player2Score++;
            logger.LogInformation("GOAL - Player 2 scores");
            logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
            ResetBall();
        }
    }

    private void UpdateBallPosition(GameTime time)
    {
        // Calculate next ball position
        float wantedNewBallPositionX = gameState.BallPosition.X + gameState.BallVerlocity.X * (float)time.DeltaTime.TotalSeconds;
        float wantedNewBallPositionY = gameState.BallPosition.Y + gameState.BallVerlocity.Y * (float)time.DeltaTime.TotalSeconds;

        // Bounce top/bottom
        if (wantedNewBallPositionY > screenConfig.ResultionY ||
            wantedNewBallPositionY < 0)
        {
            gameState.BallVerlocity = new(gameState.BallVerlocity.X, -gameState.BallVerlocity.Y);
            wantedNewBallPositionY = Math.Clamp(wantedNewBallPositionY, 0, screenConfig.ResultionY);
            logger.LogInformation("Ball bounced against top/bottom");
        }

        // Set the ball position
        gameState.BallPosition = new(wantedNewBallPositionX, wantedNewBallPositionY);
    }

    private void HandlePlayerHit(Vector2 previousBallPosition)
    {
        // Check if the ball was hit by Player 1 
        if (IntersectsPlayerWithBall(gameState.Player1Position))
        {
            HandlePlayerBounce(gameState.Player1Position, 1);
            return;
        }

        // Check if the ball was so fast it went though Player 1 
        var intersectionPlayer1 = IntersectionCalculator.GetIntersectionVector2(
            previousBallPosition,
            gameState.BallPosition,
            gameState.Player1Position.X,
            gameState.Player1Position.Y,
            pongConfig.PlayerWidth,
            pongConfig.PlayerHeight);
        if (intersectionPlayer1.lineStatus == IntersectionCalculator.Line.Entry ||
            intersectionPlayer1.lineStatus == IntersectionCalculator.Line.EntryExit)
        {
            gameState.BallPosition = new(intersectionPlayer1.EntryPoint.X, intersectionPlayer1.EntryPoint.Y);
            HandlePlayerBounce(gameState.Player1Position, 1);
            return;
        }

        // Check if the ball was hit by Player 2
        if (IntersectsPlayerWithBall(gameState.Player2Position))
        {
            HandlePlayerBounce(gameState.Player2Position, -1);
            return;
        }

        // Check if the ball was so fast it went though Player 2
        var intersectionPlayer2 = IntersectionCalculator.GetIntersectionVector2(
            previousBallPosition,
            gameState.BallPosition,
            gameState.Player2Position.X,
            gameState.Player2Position.Y,
            pongConfig.PlayerWidth,
            pongConfig.PlayerHeight);
        if (intersectionPlayer2.lineStatus == IntersectionCalculator.Line.Entry ||
            intersectionPlayer2.lineStatus == IntersectionCalculator.Line.EntryExit)
        {
            gameState.BallPosition = new(intersectionPlayer2.EntryPoint.X, intersectionPlayer2.EntryPoint.Y);
            HandlePlayerBounce(gameState.Player2Position, -1);
            return;
        }
    }

    private void HandlePlayerBounce(Vector2 playerPosition, int xDirectionModifier)
    {
        gameState.BallBounces++;
        double newballSpeed = pongConfig.BallStartSpeed + (pongConfig.BallSpeedIncrease * gameState.BallBounces);
        logger.LogInformation($"Player hits the ball. Number of player bounces: {gameState.BallBounces}, new ball speed: {newballSpeed}");

        Vector2 direction = CalculateRebounceDirection(playerPosition.Y, pongConfig.PlayerHeight, gameState.BallPosition.Y);
        gameState.BallVerlocity = new(
            (float)(xDirectionModifier * newballSpeed * direction.X),
            (float)newballSpeed * direction.Y);
        gameState.BallPosition = new(
            playerPosition.X + xDirectionModifier * (pongConfig.PlayerWidth + pongConfig.BallRadius),
            gameState.BallPosition.Y);
    }

    private Vector2 CalculateRebounceDirection(
        float playerY,
        float playerHeight,
        float ballY)
    {
        float minAngleRadians = pongConfig.PlayerMaxRebounceAngle;
        float maxAngleRadians = (float)Math.PI - pongConfig.PlayerMaxRebounceAngle;

        float radians = RemapRange(ballY, playerY, playerY + playerHeight, minAngleRadians, maxAngleRadians);
        Vector2 direction = new((float)Math.Sin(radians), -(float)Math.Cos(radians));
        return direction;
    }

    public static Vector2 Rotate(Vector2 v, double degrees)
    {
        return new Vector2(
            (float)(v.X * Math.Cos(degrees) - v.Y * Math.Sin(degrees)),
            (float)(v.X * Math.Sin(degrees) + v.Y * Math.Cos(degrees))
        );
    }


    public static float RemapRange(float from, float fromMin, float fromMax, float toMin, float toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
    }

    bool IntersectsPlayerWithBall(Vector2 playerPosition)
        => IntersectionCalculator.DoesCirlceAndRectangleIntersects(
            gameState.BallPosition.X,
            gameState.BallPosition.Y,
            pongConfig.BallRadius,
            playerPosition.X - pongConfig.PlayerWidth / 2,
            playerPosition.Y + pongConfig.PlayerHeight / 2,
            pongConfig.PlayerWidth,
            pongConfig.PlayerHeight);



    private Vector2 CalculateNewPlayerPosition(Vector2 currentPosition, double yInput, GameTime time)
    {
        if (yInput == 0.5) return currentPosition;
        float wantedMovement = (float)(yInput - 0.5) * 2 * pongConfig.PlayerSpeed;
        float newYPosition = (float)(wantedMovement * time.DeltaTime.TotalSeconds + currentPosition.Y);
        if (newYPosition < MinimumYPlayerPosition)
            newYPosition = MinimumYPlayerPosition;
        else if (newYPosition > MaximumYPlayerPosition)
        {
            newYPosition = MaximumYPlayerPosition;
        }
        return new(
            currentPosition.X,
            newYPosition);
    }
}
