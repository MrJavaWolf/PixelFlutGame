using PixelFlut.Core;

namespace PixelFlut.PingPong
{
    public static class PingPongPixelRenderer
    {
        public static int DrawFrame(
            PingPongConfiguration pingPongConfig,
            PingPongGameState gameState,
            List<PixelFlutPixel> frame)
        {
            // Draw the ball
            int frameIndexOffset = 0;
            frameIndexOffset += DrawBall(pingPongConfig, gameState, frameIndexOffset, frame);

            // Draw the players
            frameIndexOffset += DrawPlayer((int)gameState.Player1Position.X, (int)gameState.Player1Position.Y, pingPongConfig, frameIndexOffset, frame);
            frameIndexOffset += DrawPlayer((int)gameState.Player2Position.X, (int)gameState.Player2Position.Y, pingPongConfig, frameIndexOffset, frame);

            return frameIndexOffset;
        }



        private static int DrawBall(
            PingPongConfiguration pingPongConfig,
            PingPongGameState gameState,
            int frameIndexOffset,
            List<PixelFlutPixel> frame)
        {
            int numberOfPixels = 0;
            for (int x = 0; x < pingPongConfig.BallRadius * 2 + pingPongConfig.BallBorder * 2; x++)
            {
                for (int y = 0; y < pingPongConfig.BallRadius * 2 + pingPongConfig.BallBorder * 2; y++)
                {
                    int ballPixelX = (int)gameState.BallPosition.X - pingPongConfig.BallBorder - pingPongConfig.BallRadius + x;
                    int ballPixelY = (int)gameState.BallPosition.Y - pingPongConfig.BallBorder - pingPongConfig.BallRadius + y;
                    if (ballPixelX < 0 || ballPixelY < 0) continue;
                    if (x < pingPongConfig.BallBorder ||
                        y < pingPongConfig.BallBorder ||
                        x > pingPongConfig.BallRadius * 2 + pingPongConfig.BallBorder ||
                        y > pingPongConfig.BallRadius * 2 + pingPongConfig.BallBorder)
                    {
                        DrawPixelWithBackgroundColor(
                            frame,
                            frameIndexOffset + numberOfPixels,
                            ballPixelX,
                            ballPixelY);
                    }
                    else
                    {
                        DrawPixelWithBallColor(
                            frame,
                            frameIndexOffset + numberOfPixels,
                            ballPixelX,
                            ballPixelY);
                    }
                    numberOfPixels++;
                }
            }
            return numberOfPixels;
        }

        private static int DrawPlayer(
            int playerPositionX,
            int playerPositionY,
            PingPongConfiguration pingPongConfig,
            int frameIndexOffset,
            List<PixelFlutPixel> frame)
        {
            int numberOfPixels = 0;

            for (int x = 0; x < pingPongConfig.PlayerWidth + pingPongConfig.PlayerBorder * 2; x++)
            {
                for (int y = 0; y < pingPongConfig.PlayerHeight + pingPongConfig.PlayerBorder * 2; y++)
                {
                    int playerPixelX = playerPositionX - pingPongConfig.PlayerBorder + x;
                    int playerPixelY = playerPositionY - pingPongConfig.PlayerBorder + y;
                    if (playerPixelX < 0 || playerPixelY < 0) continue;
                    if (x < pingPongConfig.PlayerBorder ||
                        y < pingPongConfig.PlayerBorder ||
                        x > pingPongConfig.PlayerWidth + pingPongConfig.PlayerBorder ||
                        y > pingPongConfig.PlayerHeight + pingPongConfig.PlayerBorder)
                    {
                        DrawPixelWithBackgroundColor(
                            frame,
                            frameIndexOffset + numberOfPixels,
                            playerPixelX,
                            playerPixelY);
                    }
                    else
                    {
                        DrawPixelWithPlayerColor(
                            frame,
                            frameIndexOffset + numberOfPixels,
                            playerPixelX,
                            playerPixelY);
                    }
                    numberOfPixels++;
                }
            }
            return numberOfPixels;
        }

        private static void DrawPixelWithBallColor(
            List<PixelFlutPixel> frame,
            int index,
            int x,
            int y)
        {
            DrawPixel(
             frame,
             index,
             x,
             y,
             R: 255,
             G: 255,
             B: 255,
             A: 255);
        }

        private static void DrawPixelWithPlayerColor(
             List<PixelFlutPixel> frame,
            int index,
            int x,
            int y)
        {
            DrawPixel(
               frame,
               index,
               x,
               y,
               R: 255,
               G: 255,
               B: 255,
               A: 255);
        }

        private static void DrawPixelWithBackgroundColor(
            List<PixelFlutPixel> frame,
            int index,
            int x,
            int y)
        {
            DrawPixel(
                frame,
                index,
                x,
                y,
                R: 0,
                G: 0,
                B: 0,
                A: 255);
        }

        private static void DrawPixel(
            List<PixelFlutPixel> frame,
            int index,
            double X,
            double Y,
            byte R,
            byte G,
            byte B,
            byte A)
        {
            if (frame.Count > index)
            {
                frame[index].X = X;
                frame[index].Y = Y;
                frame[index].R = R;
                frame[index].G = G;
                frame[index].B = B;
                frame[index].A = A;
            }
            else
            {
                frame.Add(new PixelFlutPixel()
                {
                    X = X,
                    Y = Y,
                    R = R,
                    G = G,
                    B = B,
                    A = A
                });
            }
        }
    }
}
