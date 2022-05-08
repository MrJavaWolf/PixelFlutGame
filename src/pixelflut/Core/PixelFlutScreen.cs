using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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
    public int Port { get; set; }

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
    public int ResultionX { get; set; }

    /// <summary>
    /// The size of the game area
    /// </summary>
    public int ResultionY { get; set; }

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
    private List<PixelBuffer> frame = new();

    // Senders
    private List<PixelFlutScreenSender> senders = new();

    public PixelFlutScreen(
        PixelFlutScreenConfiguration configuration,
        ILogger<PixelFlutScreen> logger)
    {
        this.logger = logger;
        logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);

        for (int i = 0; i < configuration.SenderThreads; i++)
        {
            senders.Add(new PixelFlutScreenSender(configuration));
        }

        // Stats counter
        statsPrinterStopwatch.Start();
    }

    public void SetFrame(List<PixelBuffer> frame)
    {
        this.frame = frame;
        for (int i = 0; i < senders.Count; i++)
        {
            senders[i].Reset();
        }
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
            temp.BytesSent = (int)(temp.BytesSent * elasped);
            temp.PixelsSent = (int)(temp.PixelsSent * elasped);
            temp.BuffersSent = (int)(temp.BuffersSent * elasped);
            temp.FramesFromGameLoop = (int)(temp.FramesFromGameLoop * elasped);
            temp.PixelBuffersFromGameLoop = (int)(temp.PixelBuffersFromGameLoop * elasped);
            temp.PixelsFromGameLoop = (int)(temp.PixelsFromGameLoop * elasped);

            // Print
            logger.LogInformation("Screen: {@stats}", temp);
        }
    }

    public void StartRenderThreads(CancellationToken token)
    {
        for (int i = 0; i < senders.Count; i++)
        {
            PixelFlutScreenSender sender = senders[i];
            Thread thread = new(() => SenderThread(sender, token));
            thread.Start();
        }
    }

    private void SenderThread(PixelFlutScreenSender sender, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            sender.Render(frame, stats);
        }
    }
}


