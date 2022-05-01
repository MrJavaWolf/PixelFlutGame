using Microsoft.Extensions.Logging;
using PixelFlut.Core;
using System.Numerics;
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

        /// <summary>
        /// When hitting the ball on the side of the player paddle, how steep an angle (in radians) is allowed.
        /// Lowering the value will make the ball go more at an angle
        /// Recommended range:
        /// - Minimum: 0.20 (~11.5 degrees)
        /// - Maximum: 0.75 (~45 degrees)
        /// </summary>
        public float PlayerMaxRebounceAngle { get; set; }
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
        private readonly PixelFlutScreenRendererConfiguration screenConfig;
        private readonly ILogger<PingPongGame> logger;
        private PingPongGameState gameState = new();
        private List<PixelFlutPixel> frame = new();
        public int MinimumYPlayerPosition { get => 0; }
        public int MaximumYPlayerPosition { get => screenConfig.ResultionY - pingPongConfig.PlayerHeight; }

        public PingPongGame(
            PingPongConfiguration pingPongConfig,
            IPixelFlutInput input,
            PixelFlutScreenRendererConfiguration screenConfig,
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
            gameState = new();
            gameState.Player1Position = new(
                pingPongConfig.PlayerDistanceToSides,
                screenConfig.ResultionY / 2 - pingPongConfig.PlayerHeight / 2);
            gameState.Player2Position = new(
                screenConfig.ResultionX - pingPongConfig.PlayerDistanceToSides,
                screenConfig.ResultionY / 2 - pingPongConfig.PlayerHeight / 2);
            gameState.Player1Score = 0;
            gameState.Player2Score = 0;
            ResetBall();
        }

        private void ResetBall()
        {
            logger.LogInformation("Resets pingpong ball");
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
                (float)((leftRight ? -1 : 1) * pingPongConfig.BallStartSpeed * (1 - startXYBallVerlocitySplit)),
                (float)((upDown ? -1 : 1) * pingPongConfig.BallStartSpeed * startXYBallVerlocitySplit));
            gameState.BallBounces = 0;
        }

        public (int numberOfPixels, List<PixelFlutPixel> frame) Loop(GameTime time)
        {
            // Update player position
            gameState.Player1Position = CalculateNewPlayerPosition(gameState.Player1Position, input.Y, time);
            gameState.Player2Position = CalculateNewPlayerPosition(gameState.Player2Position, GetPlayer2Input(), time);

            // Update ball
            UpdateBallPosition(time);

            // Renderer
            int numberOfPixels = PingPongPixelRenderer.DrawFrame(pingPongConfig, gameState, frame);
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
                wantedNewBallPositionY = gameState.BallPosition.Y + gameState.BallVerlocity.Y * (float)time.DeltaTime.TotalSeconds;
                logger.LogInformation("Ball bounced against top/bottom");
            }

            // Set the ball position
            gameState.BallPosition = new(wantedNewBallPositionX, wantedNewBallPositionY);

            // Handle if the ball was hit by a player
            if (IntersectsPlayerWithBall(gameState.Player1Position))
                HandlePlayerBounce(gameState.Player1Position, 1);

            if (IntersectsPlayerWithBall(gameState.Player2Position))
                HandlePlayerBounce(gameState.Player2Position, -1);

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

        private void HandlePlayerBounce(Vector2 playerPosition, int xDirectionModifier)
        {
            gameState.BallBounces++;
            double newballSpeed = pingPongConfig.BallStartSpeed + (pingPongConfig.BallSpeedIncrease * gameState.BallBounces);
            logger.LogInformation($"Player hits the ball. Number of player bounces: {gameState.BallBounces}, new ball speed: {newballSpeed}");

            Vector2 direction = CalculateRebounceDirection(playerPosition.Y, pingPongConfig.PlayerHeight, gameState.BallPosition.Y);
            gameState.BallVerlocity = new(
                (float)(xDirectionModifier * newballSpeed * direction.X),
                (float)newballSpeed * direction.Y);
            gameState.BallPosition = new(
                playerPosition.X + xDirectionModifier * (pingPongConfig.PlayerWidth + pingPongConfig.BallRadius),
                gameState.BallPosition.Y);
        }

        private Vector2 CalculateRebounceDirection(
            float playerY,
            float playerHeight,
            float ballY)
        {
            float minAngleRadians = pingPongConfig.PlayerMaxRebounceAngle;
            float maxAngleRadians = (float)Math.PI - pingPongConfig.PlayerMaxRebounceAngle;

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
            => Intersects(
                gameState.BallPosition.X,
                gameState.BallPosition.Y,
                pingPongConfig.BallRadius,
                playerPosition.X - pingPongConfig.PlayerWidth / 2,
                playerPosition.Y + pingPongConfig.PlayerHeight / 2,
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

        private Vector2 CalculateNewPlayerPosition(Vector2 currentPosition, double yInput, GameTime time)
        {
            if (yInput == 0.5) return currentPosition;
            float wantedMovement = (float)(yInput - 0.5) * 2 * pingPongConfig.PlayerSpeed;
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
}
