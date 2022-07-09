using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PixelFlut.Images;

public class GameStaticImage : IGame
{

    public class Configuration
    {
        /// <summary>
        /// Path to the image to show
        /// </summary>
        public string Image { get; set; } = "";

        /// <summary>
        /// Whether the image should be scaled to the screen resolution
        /// </summary>
        public bool ScaleToResolution = true;
    }

    private record ImageFrame(List<PixelBuffer> frame, TimeSpan delay);

    private readonly ILogger<GameStaticImage> logger;
    private readonly PixelBufferFactory bufferFactory;
    private readonly Configuration config;

    // State
    private List<ImageFrame> imageFrames;
    private int imageFrameIndex = 0;
    private TimeSpan nextFrameTime = TimeSpan.Zero;

    public GameStaticImage(
        ILogger<GameStaticImage> logger,
        PixelBufferFactory bufferFactory,
        Configuration config)
    {
        this.logger = logger;
        this.bufferFactory = bufferFactory;
        this.config = config;

        // Loads image
        using Image<Rgba32> image = LoadImage();

        // Prepare frames
        imageFrames = PreprareFrames(image);
    }

    private Image<Rgba32> LoadImage()
    {
        if (!File.Exists(config.Image))
            throw new FileNotFoundException("Could not find file to display", config.Image);

        byte[] imageBytes = File.ReadAllBytes(config.Image);

        Image<Rgba32> image = Image.Load<Rgba32>(imageBytes, out IImageFormat format);
        logger.LogInformation("Image format: {@1}", format);
        // Resize the image in place and return it for chaining.
        // 'x' signifies the current image processing context.
        if (config.ScaleToResolution)
        {
            image.Mutate(x => x.Resize(bufferFactory.Screen.ResolutionX, bufferFactory.Screen.ResolutionY));
        }
        if (image.Frames.Count == 0)
        {
            throw new FileNotFoundException("Corrupt image, it appears it does not contain any frames", config.Image);
        }
        return image;
    }

    private List<ImageFrame> PreprareFrames(Image<Rgba32> image)
    {
        List<ImageFrame> frames = new();
        for (int i = 0; i < image.Frames.Count; i++)
        {
            logger.LogInformation($"Preparing frame {(i + 1)}/{image.Frames.Count}...");
            ImageFrame<Rgba32> imageFrame = image.Frames[i];
            PixelBuffer buffer = bufferFactory.Create(image.Width * image.Height);
            DrawImage(buffer, imageFrame);
            List<PixelBuffer> frame = new List<PixelBuffer>() { buffer };
            frames.Add(new ImageFrame(frame, GetFrameDelay(imageFrame)));
        }
        return frames;
    }

    private TimeSpan GetFrameDelay(ImageFrame<Rgba32> imageFrame)
    {
        var gifMetaData = imageFrame.Metadata.GetGifMetadata();
        if (gifMetaData.FrameDelay > 0)
        {
            // FrameDelay is 1/100 of a second, converts it to 1/1000 of a second
            return TimeSpan.FromMilliseconds(gifMetaData.FrameDelay * 10);
        }
        return TimeSpan.Zero;
    }

    /// <summary>
    /// Is called onces a frame
    /// </summary>
    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        // Tests if it is a still image
        if (imageFrames.Count <= 1)
            return imageFrames[imageFrameIndex].frame;

        // Do not change frame just yet
        if (time.TotalTime < nextFrameTime)
            return imageFrames[imageFrameIndex].frame;

        // Renders next frame
        imageFrameIndex++;
        if (imageFrameIndex >= imageFrames.Count)
            imageFrameIndex = 0;
        logger.LogInformation($"Renders frame: {imageFrameIndex}");
        nextFrameTime = time.TotalTime + imageFrames[imageFrameIndex].delay;
        return imageFrames[imageFrameIndex].frame;

    }

    private void DrawImage(PixelBuffer buffer, ImageFrame<Rgba32> imageFrame)
    {
        int pixelNumber = 0;
        for (int y = 0; y < bufferFactory.Screen.ResolutionY && y < imageFrame.Height; y++)
        {
            for (int x = 0; x < bufferFactory.Screen.ResolutionX && x < imageFrame.Width; x++)
            {
                Rgba32 rgb = imageFrame[x, y];
                buffer.SetPixel(pixelNumber, x, y, rgb.R, rgb.G, rgb.B, rgb.A);
                pixelNumber++;
            }
        }
    }
}
