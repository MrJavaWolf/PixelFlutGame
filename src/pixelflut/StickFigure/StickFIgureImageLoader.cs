using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace PixelFlut.StickFigure;


public class ImageFrame
{
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

    private record ImportedPixel(int x, int y, Rgba32 Color);
    public ImageFrame(ImageFrame<Rgba32> imageFrame, PixelBufferFactory bufferFactory)
    {
        pixels = new List<ImportedPixel>();
        for (int y = 0; y < imageFrame.Height; y++)
        {
            for (int x = 0; x < imageFrame.Width; x++)
            {
                Rgba32 rgb = imageFrame[x, y];
                if (rgb.A != 0)
                {
                    pixels.Add(new ImportedPixel(x, y, rgb));
                }
            }
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
            Buffer.ChangePixelPosition(
                i,
                pixel.x + (int)Position.X,
                pixel.y + (int)Position.Y);
        }
    }
}


public class ImageAnimation
{

    private List<ImageFrame> frames;
    private readonly TimeSpan timeBetweenFrames;
    private int imageFrameIndex = 0;
    private TimeSpan nextFrameTime = TimeSpan.Zero;

    public ImageAnimation(List<ImageFrame> frames, TimeSpan timeBetweenFrames)
    {
        this.frames = frames;
        this.timeBetweenFrames = timeBetweenFrames;
    }

    public PixelBuffer Loop(GameTime time)
    {
        // Checks if it is an animation
        if (frames.Count > 1 && time.TotalTime > nextFrameTime)
        {
            int prevFrameIndex = imageFrameIndex;
            // Renders next frame
            imageFrameIndex++;
            if (imageFrameIndex >= frames.Count)
                imageFrameIndex = 0;
            nextFrameTime = time.TotalTime + timeBetweenFrames;
            frames[imageFrameIndex].Position = frames[prevFrameIndex].Position;
        }
        return frames[imageFrameIndex].Buffer;
    }

    public void SetPosition(Vector2 position)
    {
        frames[imageFrameIndex].Position = position;
    }
}



public class ImageLoader
{
    private readonly PixelBufferFactory bufferFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ImageLoader> logger;

    public ImageLoader(
        PixelBufferFactory bufferFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<ImageLoader> logger)
    {
        this.bufferFactory = bufferFactory;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public Image<Rgba32> LoadImageRgb(string image, CancellationToken token)
    {
        byte[] imageBytes;
        if (image.ToLower().StartsWith("http://") || image.ToLower().StartsWith("https://"))
        {
            logger.LogInformation($"Tries to download image: {image}");
            var httpClient = httpClientFactory.CreateClient();
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var httpResponse = httpClient.GetAsync(image, token).Result; // Ugly waits for the result, should somehow be async
            logger.LogInformation($"Response status code: {httpResponse.StatusCode}");
            imageBytes = httpResponse.Content.ReadAsByteArrayAsync(token).Result;
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
        logger.LogInformation($"Number of bits in the file: {imageBytes.Count()}");
        Image<Rgba32> imageRgb = Image.Load<Rgba32>(imageBytes, out IImageFormat format);
        logger.LogInformation("Image format: {@1}", format);

        // Resizes the image to fit the resolution
        imageRgb.Mutate(x => x.Resize(bufferFactory.Screen.ResolutionX, bufferFactory.Screen.ResolutionY));
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

        for(int i = 0; i < numberOfXImages; i++)
        {
            for(int j = 0; j < numberOfYImages; j++)
            {
                images.Add(SubImage(image, numberOfXImages * i, j * numberOfYImages, width, height);
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
