using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

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
        /// Connect a controller and move the image
        /// </summary>
        public double Speed { get; set; }

        public int SizeX { get; set; }

        public int SizeY { get; set; }

        /// <summary>
        /// Auto move configurations
        /// </summary>
        public AutoMove AutoMove { get; set; } = new();

    }

    public class AutoMove
    {
        /// <summary>
        /// Should the image automatically move
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// How fast the image should move in the X direction
        /// </summary>
        public double SpeedX { get; set; }

        /// <summary>
        /// How fast the image should move in the Y direction
        /// </summary>
        public double SpeedY { get; set; }
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
    private Vector2 imagePosition = Vector2.Zero;
    private bool isMovingDown = true;
    private bool isMovingRight = true;

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
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var httpResponse = httpClient.GetAsync(config.Image, token).Result; // Ugly waits for the result, should somehow be async
            logger.LogInformation($"Response status code: {httpResponse.StatusCode}");
            imageBytes = httpResponse.Content.ReadAsByteArrayAsync(token).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
        else if (File.Exists(config.Image))
        {
            imageBytes = File.ReadAllBytes(config.Image);
        }
        else if (File.Exists(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), config.Image)))
        {
            imageBytes = File.ReadAllBytes(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), config.Image));
        }
        else
        {
            throw new FileNotFoundException("Could not find file to display", config.Image);
        }
        logger.LogInformation($"Number of bits in the file: {imageBytes.Count()}");
        Image<Rgba32> image = Image.Load<Rgba32>(imageBytes);

        // Resizes the image to fit the resolution
        image.Mutate(x => x.Resize(config.SizeX, config.SizeX));

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
            frames.Add(new ImageFrame(frame, GetGifFrameDelay(imageFrame)));
        }
        return frames;
    }

    private TimeSpan GetGifFrameDelay(ImageFrame<Rgba32> imageFrame)
    {
        var gifMetaData = imageFrame.Metadata.GetGifMetadata();
        if (gifMetaData.FrameDelay > 0)
        {
            // FrameDelay is 1/100 of a second, converts it to 1/1000 of a second
            return TimeSpan.FromMilliseconds(gifMetaData.FrameDelay * 10);
        }
        // Default gif speed: 100 ms
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

        // Change image position
        if (gamePads.Count > 0)
        {
            imagePosition.X += (float)((gamePads[0].X - 0.5) * time.DeltaTime.TotalSeconds * config.Speed);
            imagePosition.Y += (float)((gamePads[0].Y - 0.5) * time.DeltaTime.TotalSeconds * config.Speed);
            UpdateImagePosition(imageFrames[imageFrameIndex].frame[0]);
        }

        if (config.AutoMove.Enable)
        {
            AutoMoveLoop(time);
        }
        return imageFrames[imageFrameIndex].frame;

    }

    private void AutoMoveLoop(GameTime time)
    {
        if (isMovingDown)
        {
            imagePosition.Y += (float)(config.AutoMove.SpeedY * time.DeltaTime.TotalSeconds);
            if (imagePosition.Y + config.SizeY > bufferFactory.Screen.ResolutionY)
            {
                imagePosition.Y = bufferFactory.Screen.ResolutionY - config.SizeY;
                isMovingDown = false;
            }
        }
        else
        {
            imagePosition.Y -= (float)(config.AutoMove.SpeedY * time.DeltaTime.TotalSeconds);
            if (imagePosition.Y < 0)
            {
                imagePosition.Y = 0;
                isMovingDown = true;
            }
        }


        //if (isMovingRight)
        //{
        //    imagePosition.X += (float)(config.AutoMove.SpeedX * time.DeltaTime.TotalSeconds);
        //    if (imagePosition.X + config.SizeX > bufferFactory.Screen.ResolutionX)
        //    {
        //        imagePosition.X = bufferFactory.Screen.ResolutionX - config.SizeX;
        //        isMovingRight = false;
        //    }
        //}
        //else
        //{
        //    imagePosition.X -= (float)(config.AutoMove.SpeedX * time.DeltaTime.TotalSeconds);
        //    if (imagePosition.X < 0)
        //    {
        //        imagePosition.X = 0;
        //        isMovingRight = true;
        //    }
        //}

        imagePosition.X += (float)(config.AutoMove.SpeedX * time.DeltaTime.TotalSeconds);
        if (imagePosition.X > bufferFactory.Screen.ResolutionX)
        {
            imagePosition.X = 0 - config.SizeX;
        }


        UpdateImagePosition(imageFrames[imageFrameIndex].frame[0]);
    }

    private void DrawImage(PixelBuffer buffer, ImageFrame<Rgba32> imageFrame)
    {
        int pixelNumber = 0;
        for (int y = 0; y < config.SizeY && y < imageFrame.Height; y++)
        {
            for (int x = 0; x < config.SizeX && x < imageFrame.Width; x++)
            {
                Rgba32 rgb = imageFrame[x, y];
                int xPos = x + (int)imagePosition.X;
                int yPos = y + (int)imagePosition.Y;
                buffer.SetPixel(pixelNumber, xPos, yPos, rgb.R, rgb.G, rgb.B, rgb.A);
                pixelNumber++;
            }
        }
    }
    private void UpdateImagePosition(PixelBuffer buffer)
    {
        int pixelNumber = 0;
        for (int y = 0; y < config.SizeY; y++)
        {
            for (int x = 0; x < config.SizeX; x++)
            {
                int xPos = x + (int)imagePosition.X;
                int yPos = y + (int)imagePosition.Y;
                buffer.ChangePixelPosition(pixelNumber, xPos, yPos);
                pixelNumber++;
            }
        }
    }
}
