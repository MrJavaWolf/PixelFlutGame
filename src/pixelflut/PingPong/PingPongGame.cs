using Microsoft.Extensions.Logging;
using PixelFlut.Core;

namespace PixelFlut.PingPong
{

    public class PingPongConfiguration
    {
        public int BallRadius { get; set; }
        public int BallBorder { get; set; }
        public double BallStartSpeed { get; set; }
        public double BallSpeedIncrease { get; set; }
        public int PlayerHeight { get; set; }
        public int PlayerWidth { get; set; }
        public int PlayerSpeed { get; set; }
        public int PlayerBorder { get; set; }
        public int PlayerDistanceToSides { get; set; }
        public int NumberOfGoalsToWin { get; set; }
    }

    public enum PingPongGameStateType
    {
        StartScreen,
        Playing,
        Score,
    }

    public class PingPongGameState
    {
        /// <summary>
        /// Ball X Center position
        /// </summary>
        public double BallXPosition { get; set; } = 0;

        /// <summary>
        /// Ball Y Center position
        /// </summary>
        public double BallYPosition { get; set; } = 0;

        /// <summary>
        /// Ball X verlocity
        /// </summary>
        public double BallXVerlocity { get; set; } = 0;

        /// <summary>
        /// Ball Y verlocity
        /// </summary>
        public double BallYVerlocity { get; set; } = 0;

        /// <summary>
        /// Player 1's Y top-most position
        /// </summary>
        public double Player1PositionY { get; set; } = 0;

        /// <summary>
        /// Player 1's X left-most position
        /// </summary>
        public double Player1PositionX { get; set; } = 10;

        /// <summary>
        /// Player 2's Y top-most position
        /// </summary>
        public double Player2PositionY { get; set; } = 0;

        /// <summary>
        /// Player 2's X left-most position
        /// </summary>
        public double Player2PositionX { get; set; } = 490;

        /// <summary>
        /// Number of times a player have hit the ball
        /// The more times a player hits the ball the faster it goes
        /// </summary>
        public int BallBounces { get; set; } = 0;

        public PingPongGameStateType CurrentGameState { get; set; } = PingPongGameStateType.StartScreen;


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

    public class PingPongGame
    {
        private readonly PingPongConfiguration pingPongConfig;
        private readonly IPixelFlutInput input;
        private readonly PixelFlutRendererConfiguration screenConfig;
        private readonly ILogger<PingPongGame> logger;
        private PingPongGameState gameState = new();

        public int MinimumYPlayerPosition { get => 0; }
        public int MaximumYPlayerPosition { get => screenConfig.ResultionY - pingPongConfig.PlayerHeight; }

        public PingPongGame(
            PingPongConfiguration pingPongConfig,
            IPixelFlutInput input,
            PixelFlutRendererConfiguration screenConfig,
            ILogger<PingPongGame> logger,
            PingPongGameState? pingPongGameState = null)
        {
            this.pingPongConfig = pingPongConfig;
            this.input = input;
            this.screenConfig = screenConfig;
            this.logger = logger;
            if (pingPongGameState != null)
                gameState = pingPongGameState;
        }

        public void Startup()
        {
            logger.LogInformation("Initializes pingpong game");
            gameState = new PingPongGameState();
            gameState.Player1PositionY = screenConfig.ResultionY / 2 - pingPongConfig.PlayerHeight / 2;
            gameState.Player1PositionX = pingPongConfig.PlayerDistanceToSides;
            gameState.Player2PositionY = screenConfig.ResultionY / 2;
            gameState.Player2PositionX = screenConfig.ResultionX - pingPongConfig.PlayerDistanceToSides;
            gameState.Player1Score = 0;
            gameState.Player2Score = 0;
            ResetBall();
        }

        private void ResetBall()
        {
            logger.LogInformation("Resets pingpong ball");
            double startXYBallVerlocitySplit = Math.Min(0.7, Random.Shared.NextDouble());
            //double startXYBallVerlocitySplit = 0.01;
            bool leftRight = (Random.Shared.NextDouble() < 0.5 ? true : false);
            //bool leftRight = true;
            bool upDown = (Random.Shared.NextDouble() < 0.5 ? true : false);
            gameState.BallYPosition = screenConfig.ResultionY / 2;
            gameState.BallXPosition = screenConfig.ResultionX / 2;
            gameState.BallXVerlocity = (leftRight ? -1 : 1) * pingPongConfig.BallStartSpeed * (1 - startXYBallVerlocitySplit);
            gameState.BallYVerlocity = (upDown ? -1 : 1) * pingPongConfig.BallStartSpeed * (startXYBallVerlocitySplit);
            gameState.BallBounces = 0;
        }

        public List<PixelFlutPixel> Loop(GameTime time)
        {
            // Update player position
            gameState.Player1PositionY = CalculateNewPlayerPosition(gameState.Player1PositionY, input.Y, time);
            gameState.Player2PositionY = CalculateNewPlayerPosition(gameState.Player2PositionY, GetPlayer2Input(), time);

            // Update ball
            UpdateBallPosition(time);

            // Renderer
            return PingPongPixelRenderer.CreatePixels(pingPongConfig, gameState);
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

        private void UpdateBallPosition(GameTime time)
        {
            // Calculate next ball position
            double wantedNewBallPositionX = gameState.BallXPosition + gameState.BallXVerlocity * time.DeltaTime.TotalSeconds;
            double wantedNewBallPositionY = gameState.BallYPosition + gameState.BallYVerlocity * time.DeltaTime.TotalSeconds;

            // Bounce top/bottom
            if (wantedNewBallPositionY > screenConfig.ResultionY ||
                wantedNewBallPositionY < 0)
            {
                gameState.BallYVerlocity = -gameState.BallYVerlocity;
                wantedNewBallPositionY = gameState.BallYPosition + gameState.BallYVerlocity * time.DeltaTime.TotalSeconds;
                logger.LogInformation("Ball bounced against top/bottom");
            }

            // Set the ball position
            gameState.BallXPosition = wantedNewBallPositionX;
            gameState.BallYPosition = wantedNewBallPositionY;

            // Handle if the ball was hit by a player
            HandlePlayerBounce(gameState.Player1PositionX, gameState.Player1PositionY, 1);
            HandlePlayerBounce(gameState.Player2PositionX, gameState.Player2PositionY, -1);

            // Goal by player 1
            if (gameState.BallXPosition > screenConfig.ResultionX)
            {
                gameState.Player1Score++;
                logger.LogInformation("GOAL - Player 1 scores");
                logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
                ResetBall();
            }

            // Goal by player 2
            else if (gameState.BallXPosition < 0)
            {
                gameState.Player2Score++;
                logger.LogInformation("GOAL - Player 2 scores");
                logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
                ResetBall();
            }
        }

        private void HandlePlayerBounce(double playerX, double playerY, int xDirectionModifier)
        {
            if (IntersectsPlayerWithBall(
               playerX,
               playerY))
            {
                gameState.BallBounces++;
                double newballSpeed = pingPongConfig.BallStartSpeed + (pingPongConfig.BallSpeedIncrease * gameState.BallBounces);
                logger.LogInformation($"Player hits the ball. Number of player bounces: {gameState.BallBounces}, new ball speed: {newballSpeed}");
                (double xRatio, double yRatio) = CalculateRebounceDirection(playerY, pingPongConfig.PlayerHeight, gameState.BallYPosition);
                gameState.BallXVerlocity = xDirectionModifier * newballSpeed * xRatio;
                gameState.BallYVerlocity = newballSpeed * yRatio;
                gameState.BallXPosition = playerX + xDirectionModifier * (pingPongConfig.PlayerWidth + pingPongConfig.BallRadius);
            }
        }

        private (double xRatio, double yRatio) CalculateRebounceDirection(
            double playerY,
            double playerHeight,
            double ballY)
        {
            double maxSplit = 0.8;
            double minSplit = 0;
            double split;
            bool upperPart = false;
            if (ballY < playerY + playerHeight / 2)
            {
                ballY += playerHeight / 2;
                upperPart = true;
            }
            split = RemapRange(ballY, playerY + playerHeight / 2, playerY + playerHeight, minSplit, maxSplit);
            return ((1 - split), split * (upperPart ? -1 : 1));
        }


        public static double RemapRange(double value, double from1, double to1, double from2, double to2)
        {
            value = Math.Clamp(value, from1, to1);
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        bool IntersectsPlayerWithBall(double playerXPosition, double playerYPosition)
            => Intersects(
                gameState.BallXPosition,
                gameState.BallYPosition,
                pingPongConfig.BallRadius,
                playerXPosition - pingPongConfig.PlayerWidth / 2,
                playerYPosition + pingPongConfig.PlayerHeight / 2,
                pingPongConfig.PlayerWidth,
                pingPongConfig.PlayerHeight);


        bool Intersects(
            double circleX,
            double circleY,
            double circleR,
            double rectX,
            double rectY,
            double rectWidth,
            double rectHeight)
        {
            // Src: https://stackoverflow.com/a/402010
            double circleDistanceX = Math.Abs(circleX - rectX);
            double circleDistanceY = Math.Abs(circleY - rectY);
            if (circleDistanceX > (rectWidth / 2 + circleR)) { return false; }
            if (circleDistanceY > (rectHeight / 2 + circleR)) { return false; }
            if (circleDistanceX <= (rectWidth / 2)) { return true; }
            if (circleDistanceY <= (rectHeight / 2)) { return true; }
            double cornerDistance_sq = Math.Pow(circleDistanceX - rectWidth / 2, 2) + Math.Pow(circleDistanceY - rectHeight / 2, 2);
            return (cornerDistance_sq <= Math.Pow(circleR, 2));
        }

        private double CalculateNewPlayerPosition(double currentPosition, double yInput, GameTime time)
        {
            if (yInput == 0.5) return currentPosition;
            double wantedMovement = (yInput - 0.5) * 2 * pingPongConfig.PlayerSpeed;
            double newPosition = wantedMovement * time.DeltaTime.TotalSeconds + currentPosition;
            if (newPosition < MinimumYPlayerPosition)
                newPosition = MinimumYPlayerPosition;
            else if (newPosition > MaximumYPlayerPosition)
            {
                newPosition = MaximumYPlayerPosition;
            }
            return newPosition;
        }
    }
}
