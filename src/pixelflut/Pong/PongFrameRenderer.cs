using PixelFlut.Core;
namespace PixelFlut.Pong;

public class PongFrameRenderer
{
    public static int CalculatePixelsInFrame(
        PongConfiguration pongConfig,
        PongGameState gameState)
    {
        // Draw the ball
        int pixelsDrawn = 0;
        pixelsDrawn += DrawBall(pongConfig, gameState, pixelsDrawn, null);

        // Draw the players
        pixelsDrawn += DrawPlayer(
            (int)gameState.Player1Position.X,
            (int)gameState.Player1Position.Y,
            pongConfig,
            pixelsDrawn,
            null);
        pixelsDrawn += DrawPlayer(
            (int)gameState.Player2Position.X,
            (int)gameState.Player2Position.Y,
            pongConfig,
            pixelsDrawn,
            null);
        return pixelsDrawn;
    }

    public static int DrawFrame(
        PongConfiguration pongConfig,
        PongGameState gameState,
        PixelBuffer buffer)
    {
        // Draw the ball
        int pixelsDrawn = 0;
        pixelsDrawn += DrawBall(pongConfig, gameState, pixelsDrawn, buffer);

        // Draw the players
        pixelsDrawn += DrawPlayer(
            (int)gameState.Player1Position.X,
            (int)gameState.Player1Position.Y,
            pongConfig,
            pixelsDrawn,
            buffer);
        pixelsDrawn += DrawPlayer(
            (int)gameState.Player2Position.X,
            (int)gameState.Player2Position.Y,
            pongConfig,
            pixelsDrawn,
            buffer);

        return pixelsDrawn;
    }

    private static int DrawBall(
        PongConfiguration pongConfig,
        PongGameState gameState,
        int pixelOffset,
        PixelBuffer? buffer)
    {
        int numberOfPixels = 0;
        for (int x = 0; x < pongConfig.BallRadius * 2 + pongConfig.BallBorder * 2; x++)
        {
            for (int y = 0; y < pongConfig.BallRadius * 2 + pongConfig.BallBorder * 2; y++)
            {
                int ballPixelX = (int)gameState.BallPosition.X - pongConfig.BallBorder - pongConfig.BallRadius + x;
                int ballPixelY = (int)gameState.BallPosition.Y - pongConfig.BallBorder - pongConfig.BallRadius + y;
                if (ballPixelX < 0 || ballPixelY < 0)
                {
                    DrawPixelWithBackgroundColor(
                        buffer,
                        pixelOffset + numberOfPixels,
                        0,
                        0);
                }
                else if (x < pongConfig.BallBorder ||
                    y < pongConfig.BallBorder ||
                    x > pongConfig.BallRadius * 2 + pongConfig.BallBorder ||
                    y > pongConfig.BallRadius * 2 + pongConfig.BallBorder)
                {
                    DrawPixelWithBackgroundColor(
                        buffer,
                        pixelOffset + numberOfPixels,
                        ballPixelX,
                        ballPixelY);
                }
                else
                {
                    DrawPixelWithBallColor(
                        buffer,
                        pixelOffset + numberOfPixels,
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
        int pixelOffset,
        PixelBuffer? buffer)
    {
        int numberOfPixels = 0;
        for (int x = 0; x < pongConfig.PlayerWidth; x++)
        {
            for (int y = 0; y < pongConfig.PlayerHeight + pongConfig.PlayerBorder * 2; y++)
            {
                int playerPixelX = playerPositionX + x;
                int playerPixelY = playerPositionY - pongConfig.PlayerBorder + y;
                if (playerPixelY < 0)
                {
                    DrawPixelWithBackgroundColor(
                        buffer,
                        pixelOffset + numberOfPixels,
                        0,
                        0);
                }
                else if (y < pongConfig.PlayerBorder ||
                    y > pongConfig.PlayerHeight + pongConfig.PlayerBorder)
                {
                    DrawPixelWithBackgroundColor(
                        buffer,
                        pixelOffset + numberOfPixels,
                        playerPixelX,
                        playerPixelY);
                }
                else
                {
                    DrawPixelWithPlayerColor(
                        buffer,
                        pixelOffset + numberOfPixels,
                        playerPixelX,
                        playerPixelY);
                }
                numberOfPixels++;
            }
        }
        return numberOfPixels;
    }

    private static void DrawPixelWithBallColor(
        PixelBuffer? buffer,
        int pixelNumber,
        int x,
        int y)
    {
        buffer?.SetPixel(
         pixelNumber,
         x,
         y,
         R: 255,
         G: 255,
         B: 255,
         A: 255);
    }

    private static void DrawPixelWithPlayerColor(
        PixelBuffer? buffer,
        int pixelNumber,
        int x,
        int y)
    {
        buffer?.SetPixel(
           pixelNumber,
           x,
           y,
           R: 255,
           G: 255,
           B: 255,
           A: 255);
    }

    private static void DrawPixelWithBackgroundColor(
        PixelBuffer? buffer,
        int pixelNumber,
        int x,
        int y)
    {
        buffer?.SetPixel(
            pixelNumber,
            x,
            y,
            R: 0,
            G: 0,
            B: 0,
            A: 255);
    }
}
