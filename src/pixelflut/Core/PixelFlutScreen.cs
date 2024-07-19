using System.Diagnostics;

namespace PixelFlut.Core;

public class PixelFlutScreenConfiguration
{
    /// <summary>
    /// The IP of the Pixelflut
    /// </summary>
    public string Ip { get; set; } = null!;

    /// <summary>
    /// The Port of the Pixelflut
    /// </summary>
    public string Port { get; set; } = null!;

    /// <summary>
    /// Set the on screen offset if you want the game to not run from 0,0 (usually top left)
    /// </summary>
    public int OffsetX { get; set; }

    /// <summary>
    /// Set the on screen offset if you want the game to not run from 0,0 (usually top left)
    /// </summary>
    public int OffsetY { get; set; }

    /// <summary>
    /// The size of the game area
    /// </summary>
    public int ResolutionX { get; set; }

    /// <summary>
    /// The size of the game area
    /// </summary>
    public int ResolutionY { get; set; }

    /// <summary>
    /// How many threads to be dedicated to send buffers to the pixel flut server
    /// </summary>
    public int SenderThreads { get; set; }

    /// <summary>
    /// Sleep time between frames, set to -1 to run 100% CPU
    /// </summary>
    public int SleepTimeBetweenSends { get; set; }
}

public class PixelFlutScreenStats
{
    /// <summary>
    /// How many bytes is sent
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// How many pixels is sent
    /// </summary>
    public long PixelsSent { get; set; }

    /// <summary>
    /// How many buffers is sent
    /// </summary>
    public long BuffersSent { get; set; }

    /// <summary>
    /// How many buffers is sent
    /// </summary>
    public long TotalBuffersSent { get; set; }

    /// <summary>
    /// The number of times we recived a new list of pixelbuffers from the game loop
    /// </summary>
    public long FramesFromGameLoop { get; set; }

    /// <summary>
    /// The number of pixelbuffers
    /// </summary>
    public long PixelBuffersFromGameLoop { get; set; }

    /// <summary>
    /// How many pixels the gameloop have produced to be rendered
    /// </summary>
    public long PixelsFromGameLoop { get; set; }
}

public class PixelFlutScreen
{
    // Generel
    private readonly ILogger<PixelFlutScreen> logger;

    // Stats
    private Stopwatch statsPrinterStopwatch = new();
    private PixelFlutScreenStats stats = new();

    // The buffers we are currently rendering
    private List<PixelBuffer> currentFrame = new();

    public IReadOnlyList<PixelBuffer> CurrentFrame => currentFrame;

    // Senders
    private List<IPixelFlutScreenSocket> senders = new();

    public PixelFlutScreen(
        PixelFlutScreenConfiguration configuration,
        ILogger<PixelFlutScreen> logger)
    {
        this.logger = logger;
        logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);

        for (int i = 0; i < configuration.SenderThreads; i++)
        {
            senders.Add(new PixelFlutScreenUdpSocket(configuration, logger));
        }

        // Stats counter
        statsPrinterStopwatch.Start();
    }

    public void SetFrame(List<PixelBuffer> frame)
    {
        this.currentFrame = frame.ToList(); // Make a copy to ensure the caller does not change it doing sending
        stats.FramesFromGameLoop++;
        stats.PixelBuffersFromGameLoop += frame.Count;
        stats.PixelsFromGameLoop += frame.Sum(f => f.NumberOfPixels);
        PrintAndResetStats();
    }

    public void PrintAndResetStats()
    {
        if (statsPrinterStopwatch.ElapsedMilliseconds > 1000)
        {
            double elasped = statsPrinterStopwatch.Elapsed.TotalSeconds;
            PixelFlutScreenStats temp = stats;

            // Reset the stats
            long totalBuffers = stats.TotalBuffersSent;
            stats = new PixelFlutScreenStats();
            stats.TotalBuffersSent += totalBuffers;
            statsPrinterStopwatch.Restart();

            // Scale the stats to be per second
            temp.BytesSent = (long)(temp.BytesSent * elasped);
            temp.PixelsSent = (long)(temp.PixelsSent * elasped);
            temp.BuffersSent = (long)(temp.BuffersSent * elasped);
            temp.FramesFromGameLoop = (long)(temp.FramesFromGameLoop * elasped);
            temp.PixelBuffersFromGameLoop = (long)(temp.PixelBuffersFromGameLoop * elasped);
            temp.PixelsFromGameLoop = (long)(temp.PixelsFromGameLoop * elasped);

            // Print
            logger.LogInformation("Screen: {@stats}", temp);
        }
    }

    public void StartRenderThreads(CancellationToken token)
    {
        for (int i = 0; i < senders.Count; i++)
        {
            IPixelFlutScreenSocket sender = senders[i];
            Thread thread = new(() => SenderThread(sender, token));
            thread.Start();
        }
    }

    private void SenderThread(IPixelFlutScreenSocket sender, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    sender.Render(currentFrame, stats);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send package");
                Thread.Sleep(5000);
            }
        }
    }
}


