using PixelFlut.Core;
using PixelFlut.Effect;
using System.Drawing;
using System.Numerics;
namespace PixelFlut.Pong;


public class PongGame : IGame
{
    private readonly PongConfiguration pongConfig;
    private readonly PixelBufferFactory bufferFactory;
    private readonly ILogger<PongGame> logger;
    private PongGameState gameState = new();
    private List<PixelBuffer> frame = new();
    public int MinimumYPlayerPosition { get => 0; }
    public int MaximumYPlayerPosition { get => bufferFactory.Screen.ResolutionY - pongConfig.PlayerHeight; }

    public IndividualParticalExplosionEffect ballWallBounceEffect;
    public SphereExplosionEffect ballPlayerBounceEffect;
    public SphereExplosionEffect ballPositionEffect;

    public IndividualParticalExplosionEffect ballGoalEffect;

    public PongGame(
        PongConfiguration pongConfig,
        PixelBufferFactory bufferFactory,
        ILogger<PongGame> logger,
        IPixelFlutScreenProtocol screenProtocol)
    {
        this.pongConfig = pongConfig;
        this.bufferFactory = bufferFactory;
        this.logger = logger;

        ballWallBounceEffect = new(
            new(Color.White,
                Color.FromArgb(0, 0, 0, 0),
                TimeSpan.FromSeconds(0.3),
                200,
                360,
                15),
        bufferFactory);
        ballPositionEffect = new(1000, bufferFactory);
        ballPlayerBounceEffect = new(screenProtocol.PixelsPerBuffer, bufferFactory);
        ballGoalEffect = new(
            new(Color.White,
                Color.FromArgb(0, 0, 0, 0),
                TimeSpan.FromSeconds(0.5),
                700,
                360,
                50),
            bufferFactory);
        Initialize(null);
    }

    public void Initialize(PongGameState? pongGameState = null)
    {
        logger.LogInformation("Initializes game: Pong");

        frame = new List<PixelBuffer>();
        logger.LogInformation("Creates pixelbuffer for pong game...");
        int pixelsInFrame = PongFrameRenderer.CalculatePixelsInFrame(pongConfig, gameState);
        PixelBuffer buffer = bufferFactory.Create(pixelsInFrame);
        frame.Add(buffer);
        logger.LogInformation("Pixel buffer for the pong game is ready");

        if (pongGameState == null)
        {
            // Sets up a normal start game state
            gameState = new();
            gameState.Player1Position = new(
                pongConfig.PlayerDistanceToSides,
                bufferFactory.Screen.ResolutionY / 2 - pongConfig.PlayerHeight / 2);
            gameState.Player2Position = new(
                bufferFactory.Screen.ResolutionX - pongConfig.PlayerDistanceToSides,
                bufferFactory.Screen.ResolutionY / 2 - pongConfig.PlayerHeight / 2);
            gameState.Player1Score = 0;
            gameState.Player2Score = 0;
            ResetBall();
        }
        else
        {
            // Uses the provided game state
            gameState = pongGameState;
        }
    }

    private void ResetBall()
    {
        logger.LogInformation("Resets Pong ball");
        double startXYBallVerlocitySplit = Math.Min(0.7, Random.Shared.NextDouble());
        bool leftRight = Random.Shared.NextDouble() < 0.5;
        bool upDown = Random.Shared.NextDouble() < 0.5;
        float debugOffsetX = 0;
        float debugOffsetY = 0;

        //// Debug values
        //startXYBallVerlocitySplit = 0.5;
        //leftRight = true;
        //upDown = false;
        //debugOffsetX = -750;
        //debugOffsetY = -200;

        gameState.BallPosition = new Vector2(
            bufferFactory.Screen.ResolutionX / 2 + debugOffsetX,
            bufferFactory.Screen.ResolutionY / 2 + debugOffsetY);
        gameState.BallVerlocity = new Vector2(
            (float)((leftRight ? -1 : 1) * pongConfig.BallStartSpeed * (1 - startXYBallVerlocitySplit)),
            (float)((upDown ? -1 : 1) * pongConfig.BallStartSpeed * startXYBallVerlocitySplit));
        gameState.BallBounces = 0;
    }

    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        // Update player position
        if (gamePads.Count > 0)
        {
            gameState.Player1Position = CalculateNewPlayerPosition(gameState.Player1Position, gamePads[0].Y, time);
            gameState.Player2Position = CalculateNewPlayerPosition(gameState.Player2Position, GetPlayer2Input(gamePads), time);
        }

        // Update ball
        UpdateBall(time, gamePads);

        // Update effects
        UpdateEffect(ballWallBounceEffect, time);
        UpdateEffect(ballPlayerBounceEffect, time);
        UpdateEffect(ballGoalEffect, time);
        UpdateEffect(ballPositionEffect, time);
        

        // Renderer
        PongFrameRenderer.DrawFrame(pongConfig, gameState, frame[0], time);
        return frame;
    }

    private void UpdateEffect(IEffect effect, GameTime time)
    {
        if (effect.IsAlive)
        {
            effect.Loop(time);
            effect.Renderer();
            if (!frame.Contains(effect.PixelBuffer))
            {
                frame.Add(effect.PixelBuffer);
            }
        }
        else
        {
            if (frame.Contains(effect.PixelBuffer))
            {
                frame.Remove(effect.PixelBuffer);
            }
        }
    }

    private double GetPlayer2Input(IReadOnlyList<IGamePadDevice> gamePads)
    {
        if (gamePads.Count >= 2)
        {
            var player2gamepad = gamePads[1];
            return player2gamepad.Y;
        }
        else if (gamePads.Count >= 1)
        {
            // If only 1 controller is active, player 2 uses the right side of the controller
            var player1gamepad = gamePads[0];
            if (player1gamepad.NorthButton.IsPressed && !player1gamepad.SouthButton.IsPressed)
                return 0;
            else if (!player1gamepad.NorthButton.IsPressed && player1gamepad.SouthButton.IsPressed)
                return 1;
        }

        return 0.5;
    }

    private void UpdateBall(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        Vector2 previousBallPosition = gameState.BallPosition;

        // Move the ball
        UpdateBallPosition(time);

        // Handle if the ball was hit by a player
        HandlePlayerHit(previousBallPosition, time);

        // Goal by player 1
        if (gameState.BallPosition.X > bufferFactory.Screen.ResolutionX)
        {
            gameState.Player1Score++;
            logger.LogInformation("GOAL - Player 1 scores");
            logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
            ballGoalEffect.Start(gameState.BallPosition, new Vector2(-1, 0), time.TotalTime);
            ResetBall();
        }

        // Goal by player 2
        else if (gameState.BallPosition.X < 0)
        {
            gameState.Player2Score++;
            logger.LogInformation("GOAL - Player 2 scores");
            logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
            ballGoalEffect.Start(gameState.BallPosition, new Vector2(1, 0), time.TotalTime);
            ResetBall();
        }

        // Show ball effect
        if(gamePads.Any(g => g.EastButton.OnPress || g.WestButton.OnPress || g.X > 0.95 || g.X < 0.05))
        {
            Color rainbowBackground = MathHelper.ColorFromHSV(time.TotalTime.TotalSeconds * 20, 1, 1);
            ballPositionEffect.Start(time, gameState.BallPosition, 10, 75, TimeSpan.FromMilliseconds(100), Color.Black, Color.White);
        }
    }

    private void UpdateBallPosition(GameTime time)
    {
        // Calculate next ball position
        float wantedNewBallPositionX = gameState.BallPosition.X + gameState.BallVerlocity.X * (float)time.DeltaTime.TotalSeconds;
        float wantedNewBallPositionY = gameState.BallPosition.Y + gameState.BallVerlocity.Y * (float)time.DeltaTime.TotalSeconds;

        // Bounce top/bottom
        if (wantedNewBallPositionY > bufferFactory.Screen.ResolutionY ||
            wantedNewBallPositionY < 0)
        {
            gameState.BallVerlocity = new(gameState.BallVerlocity.X, -gameState.BallVerlocity.Y);
            wantedNewBallPositionY = Math.Clamp(wantedNewBallPositionY, 0, bufferFactory.Screen.ResolutionY);

            Vector2 effectDirection = gameState.BallVerlocity.Y > 0 ?
                new Vector2(0, 1) :
                new Vector2(0, -1);
            ballWallBounceEffect.Start(
                new Vector2(wantedNewBallPositionX, wantedNewBallPositionY),
                effectDirection,
                time.TotalTime);
            logger.LogInformation("Ball bounced against top/bottom");
        }

        // Set the ball position
        gameState.BallPosition = new(wantedNewBallPositionX, wantedNewBallPositionY);
    }

    private void HandlePlayerHit(Vector2 previousBallPosition, GameTime time)
    {
        // Check if the ball was hit by Player 1 
        if (IntersectsPlayerWithBall(gameState.Player1Position, 1))
        {
            HandlePlayerBounce(gameState.Player1Position, 1, time);
            gameState.Player1LastHitTime = time.TotalTime;
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
            gameState.Player1LastHitTime = time.TotalTime;
            gameState.BallPosition = new(intersectionPlayer1.EntryPoint.X, intersectionPlayer1.EntryPoint.Y);
            HandlePlayerBounce(gameState.Player1Position, 1, time);
            return;
        }

        // Check if the ball was hit by Player 2
        if (IntersectsPlayerWithBall(gameState.Player2Position, -1))
        {
            gameState.Player2LastHitTime = time.TotalTime;
            HandlePlayerBounce(gameState.Player2Position, -1, time);
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
            gameState.Player2LastHitTime = time.TotalTime;
            gameState.BallPosition = new(intersectionPlayer2.EntryPoint.X, intersectionPlayer2.EntryPoint.Y);
            HandlePlayerBounce(gameState.Player2Position, -1, time);
            return;
        }
    }

    private void HandlePlayerBounce(
        Vector2 playerPosition,
        int xDirectionModifier,
        GameTime time)
    {
        gameState.BallBounces++;
        double newballSpeed = pongConfig.BallStartSpeed + (pongConfig.BallSpeedIncrease * gameState.BallBounces);
        logger.LogInformation($"Player hits the ball. Number of player bounces: {gameState.BallBounces}, new ball speed: {newballSpeed}");


        Vector2 direction = CalculateRebounceDirection(playerPosition.Y, pongConfig.PlayerHeight, gameState.BallPosition.Y);
        gameState.BallVerlocity = new(
            (float)(xDirectionModifier * newballSpeed * direction.X),
            (float)newballSpeed * direction.Y);
        gameState.BallPosition = new(
            playerPosition.X + xDirectionModifier * (pongConfig.PlayerWidth + pongConfig.BallRadius + 1),
            gameState.BallPosition.Y);
        ballPlayerBounceEffect.Start(
            time,
            gameState.BallPosition,
            0,
            15,
            TimeSpan.FromMilliseconds(100),
            Color.FromArgb(20, 20, 20, 20),
            Color.FromArgb(255, 200, 200, 200));
    }

    private Vector2 CalculateRebounceDirection(
        float playerY,
        float playerHeight,
        float ballY)
    {
        float minAngleRadians = pongConfig.PlayerMaxRebounceAngle;
        float maxAngleRadians = (float)Math.PI - pongConfig.PlayerMaxRebounceAngle;

        float radians = MathHelper.RemapRange(ballY, playerY, playerY + playerHeight, minAngleRadians, maxAngleRadians);
        Vector2 direction = new((float)Math.Sin(radians), -(float)Math.Cos(radians));
        return direction;
    }

    bool IntersectsPlayerWithBall(Vector2 playerPosition, int xDirectionModifier)
        => IntersectionCalculator.DoesCirlceAndRectangleIntersects(
            gameState.BallPosition.X,
            gameState.BallPosition.Y,
            pongConfig.BallRadius,
            playerPosition.X + pongConfig.PlayerWidth / 2 * xDirectionModifier,
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
