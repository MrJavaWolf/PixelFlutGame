namespace PixelFlut.PingPong
{
    public static class PingPongPixelRenderer
    {
        public static List<PixelFlutPixel> CreatePixels(
            PingPongConfiguration pingPongConfig,
            PingPongGameState gameState)
        {
            List<PixelFlutPixel> pixels = new();

            // Draw the ball
            DrawBall(pingPongConfig, gameState, pixels);

            // Draw the players
            DrawPlayer((int)gameState.Player1HorizontalPosition, (int)gameState.Player1VerticalPosition, pingPongConfig, pixels);
            DrawPlayer((int)gameState.Player2HorizontalPosition, (int)gameState.Player2VerticalPosition, pingPongConfig, pixels);

            return pixels;
        }

        private static void DrawBall(PingPongConfiguration pingPongConfig, PingPongGameState gameState, List<PixelFlutPixel> pixels)
        {
            for (int x = 0; x < pingPongConfig.BallRadius + pingPongConfig.BallBorder * 2; x++)
            {
                for (int y = 0; y < pingPongConfig.BallRadius + pingPongConfig.BallBorder * 2; y++)
                {
                    int ballPixelX = (int)gameState.BallXPosition - pingPongConfig.BallBorder + x;
                    int ballPixelY = (int)gameState.BallYPosition - pingPongConfig.BallBorder + y;
                    if (ballPixelX < 0 || ballPixelY < 0) continue;
                    if (x < pingPongConfig.BallBorder ||
                        y < pingPongConfig.BallBorder ||
                        x > pingPongConfig.BallRadius + pingPongConfig.BallBorder ||
                        y > pingPongConfig.BallRadius + pingPongConfig.BallBorder)
                    {
                        pixels.Add(CreatePixelWithRandomColor(ballPixelX, ballPixelY));
                    }
                    else
                    {
                        pixels.Add(CreatePixelWithBallColor(ballPixelX, ballPixelY));
                    }
                }
            }
        }

        private static void DrawPlayer(
            int playerPositionX, 
            int playerPositionY, 
            PingPongConfiguration pingPongConfig,
            List<PixelFlutPixel> pixels)
        {
            for (int x = 0; x < pingPongConfig.PlayerWidth + pingPongConfig.PlayerBorder * 2; x++)
            {
                for (int y = 0; y < pingPongConfig.PlayerHeight + pingPongConfig.PlayerBorder * 2; y++)
                {
                    int playerPixelX = playerPositionX - pingPongConfig.PlayerBorder + x;
                    int playerPixelY = playerPositionY - pingPongConfig.PlayerBorder + y;
                    if (playerPixelX < 0 || playerPixelY < 0) continue;
                    if (
                        x < pingPongConfig.PlayerBorder ||
                        y < pingPongConfig.PlayerBorder ||
                        x > pingPongConfig.PlayerWidth + pingPongConfig.PlayerBorder ||
                        y > pingPongConfig.PlayerHeight + pingPongConfig.PlayerBorder)
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
                R = 255,
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
                //R = (byte)Random.Shared.Next(0, 255),
                //G = (byte)Random.Shared.Next(0, 255),
                //B = (byte)Random.Shared.Next(0, 255),
                R = 0,
                G = 0,
                B = 0,
                A = 255
            };
        }
    }
}
