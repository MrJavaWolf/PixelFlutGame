using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PixelFlut.Images;

public class GameImage : IGame
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
        public bool ScaleToResolution { get; set; } = true;

        /// <summary>
        /// Connect a controller and move the image
        /// </summary>
        public double Speed { get; set; }
    }

    private record ImageFrame(List<PixelBuffer> frame, TimeSpan delay);

    private readonly ILogger<GameImage> logger;
    private readonly PixelBufferFactory bufferFactory;
    private readonly Configuration config;
    private readonly StoppingToken stoppingToken;

    // State
    private List<ImageFrame> imageFrames;
    private int imageFrameIndex = 0;
    private TimeSpan nextFrameTime = TimeSpan.Zero;

    public GameImage(
        ILogger<GameImage> logger,
        PixelBufferFactory bufferFactory,
        Configuration config,
        HttpClient httpClient,
        StoppingToken stoppingToken)
    {
        this.logger = logger;
        this.bufferFactory = bufferFactory;
        this.config = config;
        this.stoppingToken = stoppingToken;

        // Loads image
        using Image<Rgba32> image = LoadImage(httpClient, stoppingToken.Token);

        // Prepare frames
        imageFrames = PreprareFrames(image, stoppingToken.Token);
    }

    private Image<Rgba32> LoadImage(HttpClient httpClient, CancellationToken token)
    {
        byte[] imageBytes;
        if (config.Image.ToLower().StartsWith("http://") || config.Image.ToLower().StartsWith("https://"))
        {
            logger.LogInformation($"Tries to download image: {config.Image}");
            var httpResponse = httpClient.GetAsync(config.Image, token).Result; // Ugly waits for the result, should somehow be async
            logger.LogInformation($"Response status code: {httpResponse.StatusCode}");
            imageBytes = httpResponse.Content.ReadAsByteArrayAsync(token).Result;
        }
        else if (File.Exists(config.Image))
        {
            imageBytes = File.ReadAllBytes(config.Image);
        }
        else
        {
            throw new FileNotFoundException("Could not find file to display", config.Image);
        }
        logger.LogInformation($"Number of bits in the file: {imageBytes.Count()}");
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

    private List<ImageFrame> PreprareFrames(Image<Rgba32> image, CancellationToken token)
    {
        List<ImageFrame> frames = new();
        for (int i = 0; i < image.Frames.Count; i++)
        {
            if (token.IsCancellationRequested) return frames;
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
        // Default gif speed = 100 ms
        return TimeSpan.FromMilliseconds(100);
    }

    /// <summary>
    /// Is called onces a frame
    /// </summary>
    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        // Checks if it is a GIF
        if (imageFrames.Count > 1 && time.TotalTime > nextFrameTime)
        {
            // Renders next frame
            imageFrameIndex++;
            if (imageFrameIndex >= imageFrames.Count)
                imageFrameIndex = 0;
            logger.LogInformation($"Renders frame: {imageFrameIndex}");
            nextFrameTime = time.TotalTime + imageFrames[imageFrameIndex].delay;
        }

        // Update frame position
        if (gamePads.Count > 0)
        {
            bufferFactory.Screen.OffsetX += (gamePads[0].X - 0.5) * time.DeltaTime.TotalSeconds * config.Speed;
            bufferFactory.Screen.OffsetY += (gamePads[0].Y - 0.5) * time.DeltaTime.TotalSeconds * config.Speed;
            UpdateImagePosition(imageFrames[imageFrameIndex].frame[0]);
        }

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
    private void UpdateImagePosition(PixelBuffer buffer)
    {
        int pixelNumber = 0;
        for (int y = 0; y < bufferFactory.Screen.ResolutionY; y++)
        {
            for (int x = 0; x < bufferFactory.Screen.ResolutionX; x++)
            {
                buffer.ChangePixelPosition(pixelNumber, x, y);
                pixelNumber++;
            }
        }
    }
}
