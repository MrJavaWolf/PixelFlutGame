namespace pixelflut
{

    public class PingPongConfiguration
    {
        public int BallSize { get; set; } = 3;

        public double BallStartSpeed { get; set; } = 5;

        public double BallSpeedIncrease { get; set; } = 0.5;

        public int PlayerHeight { get; set; } = 25;

        public int PlayerWidth { get; set; } = 2;

        public int PlayerSpeed { get; set; } = 10;

        public int PlayerDistanceToSides { get; set; } = 10;

        public int NumberOfGoalsToWin { get; set; } = 25;

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

    public class PixelflutPingPong
    {
        private readonly PingPongConfiguration pingPongConfig;
        private readonly PixelFlutGamepad gamepad;
        private readonly PixelFlutRendererConfiguration screenConfig;
        private List<PixelFlutPixel> pixels = new List<PixelFlutPixel>();

        private PingPongGameState gameState = new PingPongGameState();

        public int MinimumYPlayerPosition { get => 0; }
        public int MaximumYPlayerPosition { get => screenConfig.ResultionY - pingPongConfig.PlayerHeight; }

        public PixelflutPingPong(
            PingPongConfiguration pingPongConfig,
            PixelFlutGamepad gamepad,
            PixelFlutRendererConfiguration screenConfigu)
        {
            this.pingPongConfig = pingPongConfig;
            this.gamepad = gamepad;
            this.screenConfig = screenConfigu;
        }

        public void Startup()
        {
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
            double startXYBallVerlocitySplit = Math.Min(0.7, Random.Shared.NextDouble());
            gameState.BallYPosition = screenConfig.ResultionY / 2;
            gameState.BallXPosition = screenConfig.ResultionX / 2;
            gameState.BallXVerlocity = pingPongConfig.BallStartSpeed * (startXYBallVerlocitySplit);
            gameState.BallYVerlocity = pingPongConfig.BallStartSpeed * (1 - startXYBallVerlocitySplit);
            gameState.BallBounces = 0;
        }

        public List<PixelFlutPixel> Loop(TimeSpan time)
        {
            // Update player position
            gameState.Player1VerticalPosition = CalculateNewPlayerPosition(gameState.Player1VerticalPosition, gamepad.Y, time);
            gameState.Player2VerticalPosition = CalculateNewPlayerPosition(gameState.Player1VerticalPosition, GetPlayer2Input(), time);
            UpdateBallPosition(time);
            UpdatePixels();
            return pixels;
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

        private void UpdateBallPosition(TimeSpan time)
        {
            double wantedNewBallPositionX = gameState.BallXPosition + gameState.BallXVerlocity * time.TotalSeconds;
            double wantedNewBallPositionY = gameState.BallYPosition + gameState.BallYVerlocity * time.TotalSeconds;
            if (wantedNewBallPositionY > screenConfig.ResultionY ||
                wantedNewBallPositionY < 0)
            {
                gameState.BallYVerlocity = -gameState.BallYVerlocity;
            }

            if (wantedNewBallPositionX > screenConfig.ResultionX)
            {
                gameState.Player1Score++;
                ResetBall();
            }

            if (wantedNewBallPositionX < 0)
            {
                gameState.Player2Score++;
                ResetBall();
            }

            gameState.BallXPosition = wantedNewBallPositionX;
            gameState.BallYPosition = wantedNewBallPositionY;

            if (IntersectsPlayerWithBall(
                gameState.Player1HorizontalPosition,
                gameState.Player1VerticalPosition))
            {
                gameState.BallBounces++;
                gameState.BallXVerlocity = -gameState.BallXVerlocity;
                double ballCenter = gameState.BallYPosition + pingPongConfig.BallSize;
                double playerCenter = gameState.Player1VerticalPosition + pingPongConfig.PlayerHeight;
                double ballSpeed = pingPongConfig.BallStartSpeed * (pingPongConfig.BallStartSpeed + gameState.BallBounces);
                double maxSplit = 0.8;
                double minSplit = 0;
                if (ballCenter > playerCenter)
                {
                    double minY = playerCenter;
                    double maxY = gameState.Player1VerticalPosition + pingPongConfig.PlayerHeight;
                    double ballValue = ballCenter - playerCenter;
                    double split = RemapRange(ballValue, minY, maxY, minSplit, maxSplit);
                    gameState.BallXVerlocity = ballSpeed * (1 - split);
                    gameState.BallYVerlocity = ballSpeed * (split);
                }
                else
                {
                    double minY = gameState.Player1VerticalPosition;
                    double maxY = playerCenter;
                    double ballValue = playerCenter - ballCenter;
                    double split = RemapRange(ballValue, minY, maxY, minSplit, maxSplit);
                    gameState.BallXVerlocity = ballSpeed * (1 - split);
                    gameState.BallYVerlocity = -ballSpeed * (split);
                }
            }

            if (IntersectsPlayerWithBall(
                gameState.Player2HorizontalPosition,
                gameState.Player2VerticalPosition))
            {
                gameState.BallBounces++;
                gameState.BallXVerlocity = -gameState.BallXVerlocity;
                double ballCenter = gameState.BallYPosition + pingPongConfig.BallSize;
                double playerCenter = gameState.Player2VerticalPosition + pingPongConfig.PlayerHeight;
                double ballSpeed = pingPongConfig.BallStartSpeed * (pingPongConfig.BallStartSpeed + gameState.BallBounces);
                double maxSplit = 0.8;
                double minSplit = 0;
                if (ballCenter > playerCenter)
                {
                    double minY = playerCenter;
                    double maxY = gameState.Player2VerticalPosition + pingPongConfig.PlayerHeight;
                    double ballValue = ballCenter - playerCenter;
                    double split = RemapRange(ballValue, minY, maxY, minSplit, maxSplit);
                    gameState.BallXVerlocity = -ballSpeed * (1 - split);
                    gameState.BallYVerlocity = ballSpeed * (split);
                }
                else
                {
                    double minY = gameState.Player2VerticalPosition;
                    double maxY = playerCenter;
                    double ballValue = playerCenter - ballCenter;
                    double split = RemapRange(ballValue, minY, maxY, minSplit, maxSplit);
                    gameState.BallXVerlocity = -ballSpeed * (1 - split);
                    gameState.BallYVerlocity = -ballSpeed * (split);
                }
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
                pingPongConfig.BallSize,
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

        private double CalculateNewPlayerPosition(double currentPosition, double yInput, TimeSpan time)
        {
            if (yInput == 0.5) return currentPosition;
            double wantedMovement = (yInput - 0.5) * 2 * pingPongConfig.PlayerSpeed;
            double newPosition = wantedMovement * time.TotalSeconds + currentPosition;
            if (newPosition < MinimumYPlayerPosition)
                newPosition = MinimumYPlayerPosition;
            else if (newPosition > MaximumYPlayerPosition)
            {
                newPosition = MaximumYPlayerPosition;
            }
            return newPosition;
        }

        private void UpdatePixels()
        {
            pixels.Clear();

            // Draw the ball
            for (int x = 0; x < pingPongConfig.BallSize + 2; x++)
            {
                for (int y = 0; y < pingPongConfig.BallSize + 2; y++)
                {
                    int ballPixelX = (int)gameState.BallXPosition - 1 + x;
                    int ballPixelY = (int)gameState.BallYPosition - 1 + y;
                    if (ballPixelX < 0 || ballPixelY < 0) continue;
                    if (x == 0 || y == 0 || x == pingPongConfig.BallSize + 1 || y == pingPongConfig.BallSize + 1)
                    {
                        pixels.Add(CreatePixelWithRandomColor(ballPixelX, ballPixelY));
                    }
                    else
                    {
                        pixels.Add(CreatePixelWithBallColor(ballPixelX, ballPixelY));
                    }
                }
            }

            // Draw the players
            DrawPlayer((int)gameState.Player1HorizontalPosition, (int)gameState.Player1VerticalPosition);
            DrawPlayer((int)gameState.Player2HorizontalPosition, (int)gameState.Player2VerticalPosition);
        }

        private void DrawPlayer(int playerPositionX, int playerPositionY)
        {
            for (int x = 0; x < pingPongConfig.PlayerWidth + 2; x++)
            {
                for (int y = 0; y < pingPongConfig.PlayerHeight + 2; y++)
                {
                    int playerPixelX = (int)playerPositionX - 1 + x;
                    int playerPixelY = (int)playerPositionY - 1 + y;
                    if (playerPixelX < 0 || playerPixelY < 0) continue;
                    if (x == 0 || y == 0 || x == pingPongConfig.PlayerWidth + 1 || y == pingPongConfig.PlayerHeight + 1)
                    {
                        pixels.Add(CreatePixelWithRandomColor(playerPixelX, playerPixelY));
                    }
                    else
                    {
                        pixels.Add(CreatePixelWithPlayerColor(playerPixelX, playerPixelY));
                    }
                }
            }
        }

        private static PixelFlutPixel CreatePixelWithBallColor(int x, int y)
        {
            return new PixelFlutPixel()
            {
                X = x,
                Y = y,
                R = 0,
                G = 255,
                B = 255,
                A = 255
            };
        }

        private static PixelFlutPixel CreatePixelWithPlayerColor(int x, int y)
        {
            return new PixelFlutPixel()
            {
                X = x,
                Y = y,
                R = 255,
                G = 255,
                B = 255,
                A = 255
            };
        }

        private static PixelFlutPixel CreatePixelWithRandomColor(int x, int y)
        {
            return new PixelFlutPixel()
            {
                X = x,
                Y = y,
                R = (byte)Random.Shared.Next(0, 255),
                G = (byte)Random.Shared.Next(0, 255),
                B = (byte)Random.Shared.Next(0, 255),
                A = 255
            };
        }


    }
}
