using Humper;
using PixelFlut.Core;
using System.Drawing;

namespace StickFigureGame;

public class StickFigureWorldRenderer
{
    private readonly StickFigureGameConfiguration config;
    private readonly IPixelFlutScreenProtocol screenProtocol;
    private readonly StickFigureWorld world;
    private readonly PixelBufferFactory pixelBufferFactory;

    public StickFigureWorldRenderer(
        StickFigureGameConfiguration config,
        IPixelFlutScreenProtocol screenProtocol,
        StickFigureWorld world,
        PixelBufferFactory pixelBufferFactory)
    {
        this.config = config;
        this.screenProtocol = screenProtocol;
        this.world = world;
        this.pixelBufferFactory = pixelBufferFactory;
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        List<PixelBuffer> pixelBuffers = new List<PixelBuffer>();

        foreach (StickFigureCharacterController player in world.Players)
        {
            pixelBuffers.Add(player.StickFigureBase.PlayerAnimator.Render(time));
        }
        pixelBuffers.AddRange(RenderGround());
        return pixelBuffers;
    }

    private List<PixelBuffer> RenderGround()
    {
        List<PixelBuffer> buffers = new();
        foreach (IBox ground in world.WorldBoxes)
        {
            if (!IsGroundVisible(ground)) continue;
            buffers.Add(RenderGround(ground));
        }
        return buffers;
    }

    private bool IsGroundVisible(IBox ground)
    {
        foreach (var player in world.Players)
        {
            var c = player.Center;
            if (IsInsideCircle(c.X, c.Y, config.PlayerViewSphere, ground.X, ground.Y) ||
                IsInsideCircle(c.X, c.Y, config.PlayerViewSphere, ground.X, ground.Y + ground.Height) ||
                IsInsideCircle(c.X, c.Y, config.PlayerViewSphere, ground.X + ground.Width, ground.Y) ||
                IsInsideCircle(c.X, c.Y, config.PlayerViewSphere, ground.X + ground.Width, ground.Y + ground.Height) ||
                IsInsideCircle(c.X, c.Y, config.PlayerViewSphere, ground.X + ground.Width / 2, ground.Y + ground.Height / 2))
                return true;
        }
        return false;
    }

    bool IsInsideCircle(float circle_x, float circle_y, float rad, float x, float y)
    {
        if ((x - circle_x) * (x - circle_x) +
            (y - circle_y) * (y - circle_y) <= rad * rad)
            return true;
        else
            return false;
    }

    record PixelToRender(int x, int y);
    private PixelBuffer RenderGround(IBox ground)
    {
        int xSize = (int)(config.RenderScale * ground.Width);
        int ySize = (int)(config.RenderScale * ground.Height);

        List<PixelToRender> pixels = new();
        for (int x = 0; x < xSize; x++)
        {
            int xPos = (int)(ground.X * config.RenderScale) + x;
            for (int y = 0; y < ySize; y++)
            {
                if (IsVisible(x, y))
                {
                    int yPos = (int)(ground.Y * config.RenderScale) + y;
                    pixels.Add(new PixelToRender(xPos, yPos));
                }
            }
        }

        PixelBuffer buffer = pixelBufferFactory.Create(pixels.Count);
        int i = 0;
        foreach (PixelToRender pixelToRender in pixels)
        {
            buffer.SetPixel(i, pixelToRender.x, pixelToRender.y, Color.Green);
            i++;
        }

        return buffer;
    }

    private bool IsVisible(int x, int y)
    {
        foreach (var player in world.Players)
        {
            var c = player.Center;
            if (IsInsideCircle(c.X, c.Y, config.PlayerViewSphere, x, y))
            {
                return true;
            }
        }
        return false;
    }
}
