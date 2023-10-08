using PixelFlut.Core;
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
    private Image<Rgba32> image;

    public PixelBuffer Buffer { get; }

    private List<ImportedPixel> pixels;

    private Vector2 _Position;
    public Vector2 Position
    {
        get => _Position; set
        {
            if (_Position != value)
            {
                _Position = value;
                UpdatePosition();
            }
        }
    }

    public bool FlipX { get; set; }
    public bool FlipY { get; set; }

    public SpriteFrame(Image<Rgba32> image, PixelBufferFactory bufferFactory)
    {
        this.image = image;
        pixels = new List<ImportedPixel>();
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
        if(pixels.Count == 0)
        {
            pixels.Add(new ImportedPixel(0, 0, new Rgba32()));
        }
        this.Buffer = bufferFactory.Create(pixels.Count);
        for (int i = 0; i < pixels.Count; i++)
        {
            ImportedPixel pixel = pixels[i];
            this.Buffer.SetPixel(
                i,
                pixel.x,
                pixel.y,
                System.Drawing.Color.FromArgb(
                    pixel.Color.A,
                    pixel.Color.R,
                    pixel.Color.G,
                    pixel.Color.B));
        }
        this.Position = Vector2.Zero;
    }

    private void UpdatePosition()
    {
        for (int i = 0; i < pixels.Count; i++)
        {
            ImportedPixel pixel = pixels[i];
            int newX = (int)Position.X +
                (FlipX ? image.Width - pixel.x : pixel.x);
            int newY = (int)Position.Y +
                (FlipY ? image.Height - pixel.y : pixel.y);

            Buffer.ChangePixelPosition(i, newX, newY);
        }
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
    }

    public void Restart(GameTime time)
    {
        this.nextFrameTime = time.TotalTime + this.TimeBetweenFrames;
    }

    private bool IsAnimationDone(GameTime time) =>
        !LoopAnimation &&
        time.TotalTime > nextFrameTime &&
        animationIndex == animation.Count;

    private bool ShouldGoToNextFrame(GameTime time)
        => animation.Count > 1 &&                               // Only change frame if we have more than 1 frame
        time.TotalTime > nextFrameTime &&                       // Change frame when it is time to change frame
        (LoopAnimation || animationIndex != animation.Count);   // Only change frame we we are not on the last frame, or should loop the frame

    public List<PixelBuffer> Loop(GameTime time)
    {
        // Checks if it is an animation
        if (ShouldGoToNextFrame(time))
        {
            int prevFrameIndex = animationIndex;
            // Renders next frame
            animationIndex++;
            if (animationIndex >= animation.Count)
            {
                if (LoopAnimation)
                    animationIndex = 0;
                else
                    animationIndex--;
            }
            nextFrameTime = time.TotalTime + TimeBetweenFrames;
            frames[animation[animationIndex]].Position = frames[animation[prevFrameIndex]].Position;
            frames[animation[animationIndex]].FlipX = frames[animation[prevFrameIndex]].FlipX;
            frames[animation[animationIndex]].FlipY = frames[animation[prevFrameIndex]].FlipY;
        }
        if (IsAnimationDone(time))
            return empty;
        else
            return new List<PixelBuffer>() { frames[animation[animationIndex]].Buffer };
    }

    public void SetPosition(Vector2 position)
    {
        frames[animationIndex].Position = position;
    }

    public void SetAnimation(
        GameTime time,
        List<int> animation,
        int startAnimationIndex = 0,
        TimeSpan? timeBetweenFrames = null,
        bool loopAnimation = true)
    {
        SpriteFrame currentFrame = frames[animation[animationIndex]];
        this.LoopAnimation = loopAnimation;
        this.animation = animation;
        this.TimeBetweenFrames = timeBetweenFrames ?? this.TimeBetweenFrames;
        this.nextFrameTime = time.TotalTime + this.TimeBetweenFrames;
        this.animationIndex = startAnimationIndex;
        frames[animation[animationIndex]].Position = currentFrame.Position;
        frames[animation[animationIndex]].FlipX = currentFrame.FlipX;
        frames[animation[animationIndex]].FlipY = currentFrame.FlipY;
    }
}


public class SpriteLoader
{
    private readonly PixelBufferFactory bufferFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<SpriteLoader> logger;
    private readonly StickFigureGameConfiguration config;

    public SpriteLoader(
        PixelBufferFactory bufferFactory,
        IHttpClientFactory httpClientFactory,
        StickFigureGameConfiguration config,
        ILogger<SpriteLoader> logger)
    {
        this.bufferFactory = bufferFactory;
        this.httpClientFactory = httpClientFactory;
        this.config = config;
        this.logger = logger;
    }

    public SpriteAnimation LoadAnimation(
        string imageFile,
        int width,
        int height,
        TimeSpan? timeBetweenFrames = null,
        List<int>? animation = null,
        bool loopAnimation = true)
    {
        if (timeBetweenFrames == null)
            timeBetweenFrames = TimeSpan.FromMilliseconds(250);
        Image<Rgba32> playerIdleFullImage = LoadImageRgb(imageFile);
        List<Image<Rgba32>> playerIdleSprites = SplitImage(playerIdleFullImage, width, height);
        List<SpriteFrame> playerIdleFrames = playerIdleSprites.Select(x => new SpriteFrame(x, bufferFactory)).ToList();
        SpriteAnimation spriteAnimation = new(
            playerIdleFrames,
            timeBetweenFrames.Value,
            animation,
            loopAnimation);
        return spriteAnimation;
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

        // Resizes the image to fit the resolution
        //imageRgb.Mutate(x => x.Resize(bufferFactory.Screen.ResolutionX, bufferFactory.Screen.ResolutionY));
        if (imageRgb.Frames.Count == 0)
        {
            throw new FileNotFoundException("Corrupt image, it appears it does not contain any frames", image);
        }
        return imageRgb;
    }



    public List<Image<Rgba32>> SplitImage(Image<Rgba32> image, int width, int height)
    {
        List<Image<Rgba32>> images = new List<Image<Rgba32>>();
        int numberOfXImages = image.Width / width;
        int numberOfYImages = image.Height / height;

        for (int i = 0; i < numberOfXImages; i++)
        {
            for (int j = 0; j < numberOfYImages; j++)
            {
                images.Add(SubImage(image, numberOfXImages * i, j * numberOfYImages, width, height));
            }
        }
        return images;
    }

    public Image<Rgba32> SubImage(Image<Rgba32> image, int x, int y, int width, int height)
    {
        Image<Rgba32> subImage = new Image<Rgba32>(width, height);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                subImage[i, j] = image[i + x, j + y];
            }
        }
        return subImage;
    }
}
