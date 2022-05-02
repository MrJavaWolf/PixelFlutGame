    using PixelFlut.Core;

namespace PixelFlut.Pong;
public static class PongFrameRenderer
{
    public static int DrawFrame(
        PongConfiguration pongConfig,
        PongGameState gameState,
        List<PixelFlutPixel> frame)
    {
        // Draw the ball
        int frameIndexOffset = 0;
        frameIndexOffset += DrawBall(pongConfig, gameState, frameIndexOffset, frame);

        // Draw the players
        frameIndexOffset += DrawPlayer((int)gameState.Player1Position.X, (int)gameState.Player1Position.Y, pongConfig, frameIndexOffset, frame);
        frameIndexOffset += DrawPlayer((int)gameState.Player2Position.X, (int)gameState.Player2Position.Y, pongConfig, frameIndexOffset, frame);

        return frameIndexOffset;
    }

    private static int DrawBall(
        PongConfiguration pongConfig,
        PongGameState gameState,
        int frameIndexOffset,
        List<PixelFlutPixel> frame)
    {
        int numberOfPixels = 0;
        for (int x = 0; x < pongConfig.BallRadius * 2 + pongConfig.BallBorder * 2; x++)
        {
            for (int y = 0; y < pongConfig.BallRadius * 2 + pongConfig.BallBorder * 2; y++)
            {
                int ballPixelX = (int)gameState.BallPosition.X - pongConfig.BallBorder - pongConfig.BallRadius + x;
                int ballPixelY = (int)gameState.BallPosition.Y - pongConfig.BallBorder - pongConfig.BallRadius + y;
                if (ballPixelX < 0 || ballPixelY < 0) continue;
                if (x < pongConfig.BallBorder ||
                    y < pongConfig.BallBorder ||
                    x > pongConfig.BallRadius * 2 + pongConfig.BallBorder ||
                    y > pongConfig.BallRadius * 2 + pongConfig.BallBorder)
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
        PongConfiguration pongConfig,
        int frameIndexOffset,
        List<PixelFlutPixel> frame)
    {
        int numberOfPixels = 0;

        for (int x = 0; x < pongConfig.PlayerWidth + pongConfig.PlayerBorder * 2; x++)
        {
            for (int y = 0; y < pongConfig.PlayerHeight + pongConfig.PlayerBorder * 2; y++)
            {
                int playerPixelX = playerPositionX - pongConfig.PlayerBorder + x;
                int playerPixelY = playerPositionY - pongConfig.PlayerBorder + y;
                if (playerPixelX < 0 || playerPixelY < 0) continue;
                if (x < pongConfig.PlayerBorder ||
                    y < pongConfig.PlayerBorder ||
                    x > pongConfig.PlayerWidth + pongConfig.PlayerBorder ||
                    y > pongConfig.PlayerHeight + pongConfig.PlayerBorder)
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
