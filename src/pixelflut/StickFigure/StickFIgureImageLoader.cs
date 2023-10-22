﻿using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StickFigureGame;
using System.Numerics;

namespace PixelFlut.StickFigure;


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
        if (position == this.Position) return;
        this.Position = position;

        for (int i = 0; i < pixels.Count; i++)
        {
            ImportedPixel pixel = pixels[i];
            int newX = (int)(Position.X * stickFigureGameConfiguration.RenderScale +
                (FlipX ? originalImage.Width - pixel.x : pixel.x));

            int newY = screenConfiguration.ResolutionY - ((int)(Position.Y * stickFigureGameConfiguration.RenderScale +
                (FlipY ? originalImage.Height - pixel.y : pixel.y)));

            Buffer.ChangePixelPosition(i, newX, newY);
        }
    }

    public void SetRotation(float angle)
    {
        if (Rotation == angle) return;
        Image<Rgba32> rotatedImage = originalImage.CloneAs<Rgba32>();
        rotatedImage.Mutate(x => x.Rotate(angle));
        List<ImportedPixel> pixels = GetNonTransparentPixels(rotatedImage);
        PixelBuffer buffer = CreateBuffer(pixels, Position);
        this.Buffer = buffer;
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


public class SpriteAnimation
{
    private static readonly List<PixelBuffer> empty = new List<PixelBuffer>();

    private List<SpriteFrame> frames;

    private int animationIndex = 0;

    private TimeSpan nextFrameTime = TimeSpan.Zero;

    private IReadOnlyList<int> animation;

    public TimeSpan TimeBetweenFrames { get; set; }

    public bool LoopAnimation { get; set; }

    public bool FlipX
    {
        get => frames[animation[animationIndex]].FlipX;
        set => frames[animation[animationIndex]].FlipX = value;
    }

    public bool FlipY
    {
        get => frames[animation[animationIndex]].FlipY;
        set => frames[animation[animationIndex]].FlipY = value;
    }

    public SpriteAnimation(
        List<SpriteFrame> frames,
        TimeSpan timeBetweenFrames,
        List<int>? animation = null,
        bool loopAnimation = true)
    {
        this.frames = frames;
        this.TimeBetweenFrames = timeBetweenFrames;
        this.LoopAnimation = loopAnimation;

        if (animation != null) this.animation = animation;
        else this.animation = Enumerable.Range(0, frames.Count).ToList();
        if (this.animation.Count == 0) throw new Exception("Failed create sprite animation, animation lenght is 0");
    }

    public void Restart(GameTime time)
    {
        this.nextFrameTime = time.TotalTime + this.TimeBetweenFrames;
    }

    public bool IsAnimationDone(GameTime time) =>
        !LoopAnimation &&
        time.TotalTime > nextFrameTime &&
        animationIndex == animation.Count - 1;

    private bool ShouldGoToNextFrame(GameTime time) =>
        animation.Count > 1 &&                                      // Only change frame if we have more than 1 frame
        time.TotalTime > nextFrameTime &&                           // Change frame when it is time to change frame
        (LoopAnimation || animationIndex != animation.Count - 1);   // Only change frame we we are not on the last frame, or should loop the frame

    public List<PixelBuffer> Render(GameTime time)
    {
        // Checks if it is an animation
        if (ShouldGoToNextFrame(time))
        {
            SpriteFrame previousFrame = frames[animation[animationIndex]];

            // Renders next frame
            animationIndex++;
            if (animationIndex >= animation.Count)
            {
                if (LoopAnimation)
                    animationIndex = 0;
                else
                    animationIndex--;
            }
            UpdateAnimationIndex(
                animationIndex,
                time,
                previousFrame.Position,
                previousFrame.FlipX,
                previousFrame.FlipY,
                previousFrame.Rotation);

        }
        if (IsAnimationDone(time))
            return empty;
        else
            return new List<PixelBuffer>() { frames[animation[animationIndex]].Buffer };
    }

    public void SetPosition(Vector2 position)
    {
        frames[animation[animationIndex]].SetPosition(position);
    }

    public void SetRotation(float rotation)
    {
        frames[animation[animationIndex]].SetRotation(rotation);
    }

    public void SetAnimation(
        GameTime time,
        List<int> animation,
        int startAnimationIndex = 0,
        TimeSpan? timeBetweenFrames = null,
        bool loopAnimation = true)
    {
        SpriteFrame previousFrame = frames[animation[animationIndex]];
        this.LoopAnimation = loopAnimation;
        this.animation = animation;
        this.TimeBetweenFrames = timeBetweenFrames ?? this.TimeBetweenFrames;

        UpdateAnimationIndex(
            startAnimationIndex,
            time,
            previousFrame.Position,
            previousFrame.FlipX,
            previousFrame.FlipY,
            previousFrame.Rotation);
    }

    public void UpdateAnimationIndex(
        int toIndex,
        GameTime time,
        Vector2 position,
        bool flipX,
        bool flipY,
        float rotation)
    {
        frames[animation[toIndex]].FlipX = flipX;
        frames[animation[toIndex]].FlipY = flipY;
        frames[animation[toIndex]].SetRotation(rotation);
        frames[animationIndex].SetPosition(position);

        this.animationIndex = toIndex;
        this.nextFrameTime = time.TotalTime + this.TimeBetweenFrames;
    }
}


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

    public SpriteAnimation LoadAnimation(
        string imageFile,
        int width,
        int height,
        float pixelsPerUnit,
        TimeSpan? timeBetweenFrames = null,
        List<int>? animation = null,
        bool loopAnimation = true,
        Vector4? cropEachSprite = null)
    {
        if (timeBetweenFrames == null)
            timeBetweenFrames = TimeSpan.FromMilliseconds(250);

        if (cropEachSprite == null)
            cropEachSprite = Vector4.Zero;

        Image<Rgba32> fullimage = LoadImageRgb(imageFile);
        List<Image<Rgba32>> sprites = SplitImage(fullimage, width, height, cropEachSprite.Value);
        ResizeImages(sprites, pixelsPerUnit);
        List<SpriteFrame> frames = sprites.Select(x => new SpriteFrame(x, bufferFactory, config, screenConfiguration)).ToList();
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
                (int)((sprite.Width / pixelsPerUnit) * config.RenderScale),
                (int)((sprite.Height / pixelsPerUnit) * config.RenderScale)));
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
        else
        {
            throw new FileNotFoundException("Could not find file to display", image);
        }
        logger.LogInformation($"Number of bytes in the file: {imageBytes.Count()} b - {image}");
        Image<Rgba32> imageRgb = Image.Load<Rgba32>(imageBytes, out IImageFormat format);
        logger.LogInformation("Image format: {@1}", format);

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

        for (int i = 0; i < numberOfXImages; i++)
        {
            for (int j = 0; j < numberOfYImages; j++)
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
                //if (x < cropEachSprite.X ||
                //    x > cropEachSprite.Z ||
                //    y < cropEachSprite.Y ||
                //    y > cropEachSprite.W)
                //    continue;

                subImage[x - (int)cropEachSprite.X, y - (int)cropEachSprite.Y] = 
                    image[startX + x, startY + y];
            }
        }
        return subImage;
    }
}
