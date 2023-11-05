using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
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

    private System.Drawing.Color? overwriteColor;
    private int outlineSize = 0;
    public SpriteFrame(
        Image<Rgba32> image,
        PixelBufferFactory bufferFactory,
        StickFigureGameConfiguration stickFigureGameConfiguration,
        PixelFlutScreenConfiguration screenConfiguration,
        System.Drawing.Color? overwriteColor = null,
        int outlineSize = 0)
    {
        originalImage = image;
        this.bufferFactory = bufferFactory;
        this.screenConfiguration = screenConfiguration;
        this.overwriteColor = overwriteColor;
        this.outlineSize = outlineSize;
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

    private List<ImportedPixel> GetNonTransparentPixels(Image<Rgba32> image)
    {
        List<ImportedPixel> pixels = new();

        Rgba32? overwriteRgb = null;
        Rgba32 outlineRgb = new Rgba32(0, 0, 0, 255);

        if (overwriteColor != null)
        {
            overwriteRgb = new Rgba32(
                overwriteColor.Value.R,
                overwriteColor.Value.G,
                overwriteColor.Value.B,
                overwriteColor.Value.A);
        }

        for (int i = 0; i < image.Height; i++)
        {
            for (int j = 0; j < image.Width; j++)
            {
                Rgba32 rgb = image[j, i];
                if (rgb.A != 0)
                {
                    if (overwriteRgb != null)
                    {
                        rgb = overwriteRgb.Value;
                    }
                    pixels.Add(new ImportedPixel(j, i, rgb));
                }
                else if (outlineSize > 0)
                {
                    // Check if a drawn pixel within the specified distance
                    bool isWithinDistance = false;

                    for (int x = Math.Max(0, i - outlineSize); x <= Math.Min(image.Height - 1, i + outlineSize); x++)
                    {
                        for (int y = Math.Max(0, j - outlineSize); y <= Math.Min(image.Width - 1, j + outlineSize); y++)
                        {
                            if (image[y, x].A != 0)
                            {
                                isWithinDistance = true;
                                break;
                            }
                        }

                        if (isWithinDistance)
                        {
                            break;
                        }
                    }

                    if (isWithinDistance)
                    {
                        pixels.Add(new ImportedPixel(j, i, outlineRgb));
                    }
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