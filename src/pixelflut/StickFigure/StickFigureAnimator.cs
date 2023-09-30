using PixelFlut.Core;
using System.Drawing;

namespace StickFigureGame;

public enum StickFigureAnimation
{
    Idle,
    Shoot,
    Run,
    JumpTop,
    JumpUp,
    JumpDown,
    SwordAttack,
    Dash,
    TakeDamage,

}

public class StickFigureAnimator
{
    private readonly IPixelFlutScreenProtocol screenProtocol;
    private readonly StickFigureBase stickFigureBase;



    public bool FlipX { get; set; }

    public StickFigureAnimator(
        IPixelFlutScreenProtocol screenProtocol,
        StickFigureBase stickFigureBase)
    {
        this.screenProtocol = screenProtocol;
        this.stickFigureBase = stickFigureBase;
    }


    public void Play(StickFigureAnimation animation)
    {

    }

    public PixelBuffer Render(PixelBufferFactory pixelBufferFactory, float renderScale)
    {
        int playerBorder = 2;
        int xSize = (int)(renderScale * stickFigureBase.Size.X) + playerBorder * 2;
        int ySize = (int)(renderScale * stickFigureBase.Size.Y) + playerBorder * 2;
        int numberOfPixels = xSize * ySize;

        PixelBuffer buffer = pixelBufferFactory.Create(numberOfPixels);
        for (int x = 0; x < xSize; x++)
        {
            int xPos = (int)(stickFigureBase.Position.X * renderScale) + x;
            for (int y = 0; y < ySize; y++)
            {
                int yPos = (int)(stickFigureBase.Position.Y * renderScale) + y;
                if (x > playerBorder && x < xSize - playerBorder &&
                    y > playerBorder && y < ySize - playerBorder)
                {
                    buffer.SetPixel(x * ySize + y, xPos, yPos, Color.Pink);
                }
                else
                {
                    buffer.SetPixel(x * ySize + y, xPos, yPos, Color.Black);
                }
            }
        }
        return buffer;
    }
}
