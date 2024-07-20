using Humper;
using PixelFlut.Core;
using System.Drawing;

namespace StickFigureGame;

public class StickFigureWorldRenderer
{
    private readonly StickFigureGameConfiguration config;
    private readonly StickFigureWorld world;
    private readonly PixelBufferFactory pixelBufferFactory;
    private readonly PixelFlutScreenConfiguration screenConfiguration;

    private List<PixelBuffer> prerenderedGroundPixelBuffers;


    public StickFigureWorldRenderer(
        StickFigureGameConfiguration config,
        PixelFlutScreenConfiguration screenConfiguration,
        StickFigureWorld world,
        PixelBufferFactory pixelBufferFactory)
    {
        this.config = config;
        this.screenConfiguration = screenConfiguration;
        this.world = world;
        this.pixelBufferFactory = pixelBufferFactory;
        prerenderedGroundPixelBuffers = RenderGround();
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        List<PixelBuffer> pixelBuffers = new List<PixelBuffer>();

        pixelBuffers.AddRange(prerenderedGroundPixelBuffers);


        foreach (StickFigureCharacterController player in world.Players)
        {
            pixelBuffers.AddRange(player.StickFigureBase.PlayerAnimator.Render(time));
            pixelBuffers.AddRange(player.SlashAnimator.Loop(time));
        }

        for (int i = 0; i < world.Projectiles.Count; i++)
        {
            pixelBuffers.AddRange(world.Projectiles[i].Animator.Render(time));
        }

        for (int i = 0; i < world.Explosions.Count; i++)
        {
            pixelBuffers.AddRange(world.Explosions[i].Animator.Render(time));
        }


        return pixelBuffers;
    }

    private List<PixelBuffer> RenderGround()
    {
        List<PixelBuffer> buffers = new();
        foreach (IBox ground in world.WorldBoxes)
        {
            //if (!IsGroundVisible(ground)) continue;
            buffers.Add(RenderGround(ground));
        }
        return buffers;
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
                int yPos = (int)(ground.Y * config.RenderScale) + y;
                pixels.Add(new PixelToRender(xPos, screenConfiguration.ResolutionY - yPos));
            }
        }
        if (pixels.Count == 0)
        {
            pixels.Add(new PixelToRender(0, 0));
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
}
