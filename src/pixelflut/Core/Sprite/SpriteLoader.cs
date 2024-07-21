using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StickFigureGame;
using System.Numerics;

namespace PixelFlut.Core.Sprite;


public class SpriteLoader
{
    private readonly PixelBufferFactory bufferFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<SpriteLoader> logger;
    private readonly StickFigureGameConfiguration config;
    private readonly PixelFlutScreenConfiguration screenConfiguration;

    public SpriteLoader(
        PixelBufferFactory bufferFactory,
        IHttpClientFactory httpClientFactory,
        StickFigureGameConfiguration config,
        PixelFlutScreenConfiguration screenConfiguration,
        ILogger<SpriteLoader> logger)
    {
        this.bufferFactory = bufferFactory;
        this.httpClientFactory = httpClientFactory;
        this.config = config;
        this.screenConfiguration = screenConfiguration;
        this.logger = logger;
    }

    public int SpriteToUnitConversion(int spritePixelSize, float wantedSizeInUnits)
    {
        return (int)(spritePixelSize / wantedSizeInUnits);
    }

    public SpriteAnimation LoadAnimation(
        string imageFile,
        int width,
        int height,
        float pixelsPerUnit,
        TimeSpan? timeBetweenFrames = null,
        List<int>? animation = null,
        bool loopAnimation = true,
        Vector4? cropEachSprite = null,
        System.Drawing.Color? overwriteColor = null,
        int outlineSize = 0)
    {
        if (timeBetweenFrames == null)
            timeBetweenFrames = TimeSpan.FromMilliseconds(250);

        if (cropEachSprite == null)
            cropEachSprite = Vector4.Zero;

        Image<Rgba32> fullimage = LoadImageRgb(imageFile);
        List<Image<Rgba32>> sprites = SplitImage(fullimage, width, height, cropEachSprite.Value);
        ResizeImages(sprites, pixelsPerUnit);
        List<SpriteFrame> frames = sprites.Select(x => new SpriteFrame(x, bufferFactory, config, screenConfiguration, overwriteColor, outlineSize)).ToList();
        SpriteAnimation spriteAnimation = new(
            frames,
            timeBetweenFrames.Value,
            animation,
            loopAnimation);
        return spriteAnimation;
    }

    private void ResizeImages(List<Image<Rgba32>> sprites, float pixelsPerUnit)
    {
        foreach (Image<Rgba32> sprite in sprites)
        {
            sprite.Mutate(x => x.Resize(
                (int)(sprite.Width / pixelsPerUnit * config.RenderScale),
                (int)(sprite.Height / pixelsPerUnit * config.RenderScale)));
        }
    }

    public Image<Rgba32> LoadImageRgb(string image)
    {
        byte[] imageBytes;
        if (image.ToLower().StartsWith("http://") || image.ToLower().StartsWith("https://"))
        {
            logger.LogInformation($"Tries to download image: {image}");
            var httpClient = httpClientFactory.CreateClient();
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            // Ugly waits for the result, should use async, but this is easiere
            var httpResponse = httpClient.GetAsync(image).Result;
            logger.LogInformation($"Response status code: {httpResponse.StatusCode}");
            imageBytes = httpResponse.Content.ReadAsByteArrayAsync().Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
        else if (File.Exists(image))
        {
            imageBytes = File.ReadAllBytes(image);
        }
        else if (File.Exists(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), image)))
        {
            imageBytes = File.ReadAllBytes(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), image));

        }
        else
        {
            throw new FileNotFoundException("Could not find file to display", image);
        }
        logger.LogInformation($"Number of bytes in the file: {imageBytes.Count()} b - {image}");
        Image<Rgba32> imageRgb = Image.Load<Rgba32>(imageBytes);

        if (imageRgb.Frames.Count == 0)
        {
            throw new FileNotFoundException("Corrupt image, it appears it does not contain any frames", image);
        }
        return imageRgb;
    }



    public List<Image<Rgba32>> SplitImage(
        Image<Rgba32> image,
        int width,
        int height,
        Vector4 cropEachSprite)
    {
        List<Image<Rgba32>> images = new List<Image<Rgba32>>();
        int numberOfXImages = image.Width / width;
        int numberOfYImages = image.Height / height;

        for (int j = 0; j < numberOfYImages; j++)
        {
            for (int i = 0; i < numberOfXImages; i++)
            {
                images.Add(SubImage(
                    image,
                    i * width,
                    j * height,
                    width,
                    height,
                    cropEachSprite));
            }
        }
        return images;
    }

    public Image<Rgba32> SubImage(
        Image<Rgba32> image,
        int startX,
        int startY,
        int width,
        int height,
        Vector4 cropEachSprite)
    {
        int widthCrop = (int)(cropEachSprite.X + (width - (width - cropEachSprite.Z)));
        int heightCrop = (int)(cropEachSprite.Y + (height - (height - cropEachSprite.W)));
        Image<Rgba32> subImage = new Image<Rgba32>(width - widthCrop, height - heightCrop);
        for (int x = (int)cropEachSprite.X; x < width - (int)cropEachSprite.Z; x++)
        {
            for (int y = (int)cropEachSprite.Y; y < height - (int)cropEachSprite.W; y++)
            {
                subImage[x - (int)cropEachSprite.X, y - (int)cropEachSprite.Y] =
                    image[startX + x, startY + y];
            }
        }
        return subImage;
    }
}

