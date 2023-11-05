using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StickFigureGame;
using System.Numerics;

namespace PixelFlut.Core.Sprite;


public class SpriteFrame
{
    private record ImportedPixel(int x, int y, Rgba32 Color);
    private Image<Rgba32> originalImage;

    public PixelBuffer Buffer { get; private set; }

    private List<ImportedPixel> pixels;

    public Vector2 Position { get; private set; }
    public bool FlipX { get; set; }
    public bool FlipY { get; set; } = true;
    public float Rotation { get; private set; }


    private PixelBufferFactory bufferFactory;

    private PixelFlutScreenConfiguration screenConfiguration;

    private StickFigureGameConfiguration stickFigureGameConfiguration;

    public SpriteFrame(
        Image<Rgba32> image,
        PixelBufferFactory bufferFactory,
        StickFigureGameConfiguration stickFigureGameConfiguration,
        PixelFlutScreenConfiguration screenConfiguration)
    {
        originalImage = image;
        this.bufferFactory = bufferFactory;
        this.screenConfiguration = screenConfiguration;
        this.stickFigureGameConfiguration = stickFigureGameConfiguration;
        pixels = GetNonTransparentPixels(image);
        Buffer = CreateBuffer(pixels, Position);
    }

    public void SetPosition(Vector2 position)
    {
        //if (position == Position) return;
        Position = position;

        for (int i = 0; i < pixels.Count; i++)
        {
            ImportedPixel pixel = pixels[i];
            int newX = (int)(Position.X * stickFigureGameConfiguration.RenderScale +
                (FlipX ? originalImage.Width - pixel.x : pixel.x));

            int newY = screenConfiguration.ResolutionY - (int)(Position.Y * stickFigureGameConfiguration.RenderScale +
                (FlipY ? originalImage.Height - pixel.y : pixel.y));

            Buffer.ChangePixelPosition(i, newX, newY);
        }
    }

    public void SetRotation(float angle)
    {
        //if (Rotation == angle) return;
        Image<Rgba32> rotatedImage = originalImage.CloneAs<Rgba32>();
        rotatedImage.Mutate(x => x.Rotate(angle));
        pixels = GetNonTransparentPixels(rotatedImage);
        PixelBuffer buffer = CreateBuffer(pixels, Position);
        Buffer = buffer;
        Rotation = angle;
    }

    private PixelBuffer CreateBuffer(List<ImportedPixel> pixels, Vector2 position)
    {
        var buffer = bufferFactory.Create(pixels.Count);
        for (int i = 0; i < pixels.Count; i++)
        {
            ImportedPixel pixel = pixels[i];
            buffer.SetPixel(
                i,
                pixel.x + (int)position.X,
                pixel.y + (int)position.Y,
                System.Drawing.Color.FromArgb(
                    pixel.Color.A,
                    pixel.Color.R,
                    pixel.Color.G,
                    pixel.Color.B));
        }
        return buffer;
    }

    private static List<ImportedPixel> GetNonTransparentPixels(Image<Rgba32> image)
    {
        List<ImportedPixel> pixels = new();
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 rgb = image[x, y];
                if (rgb.A != 0)
                {
                    pixels.Add(new ImportedPixel(x, y, rgb));
                }
            }
        }

        if (pixels.Count == 0)
        {
            // Always have at least one pixel, the rest of the system expect at least one pixel 
            // Correct solution: do not load and use the image
            pixels.Add(new ImportedPixel(0, 0, new Rgba32()));
        }

        return pixels;
    }
}