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
        public double BallXPosition { get; set; } = 0;
        public double BallYPosition { get; set; } = 0;
        public double BallXVerlocity { get; set; } = 0;
        public double BallYVerlocity { get; set; } = 0;
        public double Player1VerticalPosition { get; set; } = 0;
        public double Player1HorizontalPosition { get; set; } = 10;
        public double Player2VerticalPosition { get; set; } = 0;
        public double Player2HorizontalPosition { get; set; } = 490;
        public PingPongGameStateType CurrentGameState { get; set; } = PingPongGameStateType.StartScreen;
        public int PreviousWinner { get; set; } = -1;
        public int Player1Score { get; set; } = 0;
        public int Player2Score { get; set; } = 0;
        public int BallBounces { get; set; } = 0;

    }

    public class PingPongGame
    {
        private readonly PingPongConfiguration pingPongConfig;
        private readonly PixelFlutGamepad gamepad;
        private readonly PixelFlutRendererConfiguration screenConfig;
        private readonly ILogger<PingPongGame> logger;
        private PingPongGameState gameState = new();

        public int MinimumYPlayerPosition { get => 0; }
        public int MaximumYPlayerPosition { get => screenConfig.ResultionY - pingPongConfig.PlayerHeight; }

        public PingPongGame(
            PingPongConfiguration pingPongConfig,
            PixelFlutGamepad gamepad,
            PixelFlutRendererConfiguration screenConfig,
            ILogger<PingPongGame> logger)
        {
            this.pingPongConfig = pingPongConfig;
            this.gamepad = gamepad;
            this.screenConfig = screenConfig;
            this.logger = logger;
        }

        public void Startup()
        {
            logger.LogInformation("Initializes pingpong game");
            gameState = new PingPongGameState();
            gameState.Player1VerticalPosition = screenConfig.ResultionY / 2;
            gameState.Player1HorizontalPosition = pingPongConfig.PlayerDistanceToSides;
            gameState.Player2VerticalPosition = screenConfig.ResultionY / 2;
            gameState.Player2HorizontalPosition = screenConfig.ResultionX - pingPongConfig.PlayerDistanceToSides;
            gameState.Player1Score = 0;
            gameState.Player2Score = 0;
            ResetBall();
        }

        private void ResetBall()
        {
            logger.LogInformation("Resets pingpong ball");
            //double startXYBallVerlocitySplit = Math.Min(0.7, Random.Shared.NextDouble());
            double startXYBallVerlocitySplit = 0;
            //bool leftRight = (Random.Shared.NextDouble() < 0.5 ? true : false);
            bool leftRight = true;
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
            gameState.Player1VerticalPosition = CalculateNewPlayerPosition(gameState.Player1VerticalPosition, gamepad.Y, time);
            gameState.Player2VerticalPosition = CalculateNewPlayerPosition(gameState.Player2VerticalPosition, GetPlayer2Input(), time);
            UpdateBallPosition(time);
            return PingPongPixelRenderer.CreatePixels(pingPongConfig, gameState);
        }

        private double GetPlayer2Input()
        {
            if (gamepad.IsNorthButtonPressed && !gamepad.IsSouthButtonPressed)
                return 0;
            else if (!gamepad.IsNorthButtonPressed && gamepad.IsSouthButtonPressed)
                return 1;
            else
                return 0.5;
        }

        private void UpdateBallPosition(GameTime time)
        {
            double wantedNewBallPositionX = gameState.BallXPosition + gameState.BallXVerlocity * time.DeltaTime.TotalSeconds;
            double wantedNewBallPositionY = gameState.BallYPosition + gameState.BallYVerlocity * time.DeltaTime.TotalSeconds;
            if (wantedNewBallPositionY > screenConfig.ResultionY ||
                wantedNewBallPositionY < 0)
            {
                gameState.BallYVerlocity = -gameState.BallYVerlocity;
                wantedNewBallPositionY = gameState.BallYPosition + gameState.BallYVerlocity * time.DeltaTime.TotalSeconds;
                logger.LogInformation("Ball hit up/down side");
            }

            gameState.BallXPosition = wantedNewBallPositionX;
            gameState.BallYPosition = wantedNewBallPositionY;


            HandlePlayerBounce(gameState.Player1HorizontalPosition, gameState.Player1VerticalPosition, 1);
            HandlePlayerBounce(gameState.Player2HorizontalPosition, gameState.Player2VerticalPosition, -1);

            if (gameState.BallXPosition > screenConfig.ResultionX)
            {
                gameState.Player1Score++;
                logger.LogInformation("GOAL - Player 1 scores");
                logger.LogInformation($"Player 1: {gameState.Player1Score} VS Player 2: {gameState.Player2Score}");
                ResetBall();
            }
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
                double ballCenter = gameState.BallYPosition + pingPongConfig.BallRadius;
                double playerCenter = playerY + pingPongConfig.PlayerHeight / 2;
                double maxSplit = 0.8;
                double minSplit = 0;
                if (ballCenter > playerCenter)
                {
                    double minY = playerCenter;
                    double maxY = playerY + pingPongConfig.PlayerHeight;
                    double ballValue = Math.Max(maxY, Math.Min(minY, ballCenter));
                    double split = RemapRange(ballValue, minY, maxY, minSplit, maxSplit);
                    gameState.BallXVerlocity = xDirectionModifier * newballSpeed * (split);
                    gameState.BallYVerlocity = newballSpeed * (1 - split);
                }
                else
                {
                    double minY = playerY;
                    double maxY = playerCenter;
                    double ballValue = Math.Max(maxY, Math.Min(minY, ballCenter));
                    double split = RemapRange(ballValue, minY, maxY, minSplit, maxSplit);
                    gameState.BallXVerlocity = xDirectionModifier * newballSpeed * (split);
                    gameState.BallYVerlocity = -newballSpeed * (1 - split);
                }
                gameState.BallXPosition = playerX + xDirectionModifier * (pingPongConfig.PlayerWidth + pingPongConfig.BallRadius);
            }
        }


        public static double RemapRange(double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        bool IntersectsPlayerWithBall(double playerXPosition, double playerYPosition)
            => Intersects(
                gameState.BallXPosition,
                gameState.BallYPosition,
                pingPongConfig.BallRadius,
                playerXPosition,
                playerYPosition,
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
