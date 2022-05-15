using Microsoft.Extensions.DependencyInjection;
using PixelFlut.Pong;
using System.Diagnostics;

namespace PixelFlut.Core;

public class GameLoopConfiguration
{
    /// <summary>
    /// How many time a second the game loop will run
    /// </summary>
    public double TargetGameLoopFPS { get; set; }


    /// <summary>
    /// Whether it should render a test image insteed of playing a game
    /// </summary>
    public TestImageType TestImage { get; set; }

    /// <summary>
    /// The test image offset
    /// </summary>
    public double TestImageOffset { get; set; }

    public enum TestImageType
    {
        Disable,
        Still,
        Moving,
        Black
    }
}

public class GameLoopStats
{
    public GameTime Time { get; set; } = new();
    public int SleepTime { get; set; }
    public int Frames { get; set; }
}

public class GameLoop
{
    // Generel
    private readonly ILogger<GameLoop> logger;
    private readonly PixelFlutScreen renderer;
    private readonly TestFrameGenerator testFraneGenerator;
    private readonly IServiceProvider provider;
    private readonly GameLoopConfiguration configuration;

    // Stats
    private GameLoopStats stats = new();
    private Stopwatch statsPrinterStopwatch = new();

    public GameLoop(
        ILogger<GameLoop> logger,
        PixelFlutScreen renderer,
        TestFrameGenerator testFraneGenerator,
        IServiceProvider provider,
        GameLoopConfiguration configuration)
    {
        this.logger = logger;
        this.renderer = renderer;
        this.testFraneGenerator = testFraneGenerator;
        this.provider = provider;
        this.configuration = configuration;
        logger.LogInformation($"GameLoop: {{@configuration}}", configuration);
        statsPrinterStopwatch.Start();
    }

    public void Run(CancellationToken cancellationToken)
    {
        // Start the renderer
        renderer.StartRenderThreads(cancellationToken);

        switch (configuration.TestImage)
        {
            case GameLoopConfiguration.TestImageType.Disable:
                RunGameLoop(cancellationToken);
                break;
            case GameLoopConfiguration.TestImageType.Still:
                testFraneGenerator.Startup();
                RenderStillTestImage(cancellationToken);
                break;
            case GameLoopConfiguration.TestImageType.Moving:
                testFraneGenerator.Startup();
                RenderMovingTestImage(cancellationToken);
                break;
            case GameLoopConfiguration.TestImageType.Black:
                testFraneGenerator.Startup();
                RenderBlackTestImage(cancellationToken);
                break;

        }
    }

    private void RunGameLoop(CancellationToken cancellationToken)
    {
        // Setup the timers
        Stopwatch loopTime = new();
        Stopwatch totalGameTimer = new();
        GameTime gameTime = new();
        totalGameTimer.Start();

        // Setup the game
        PongGame pong = provider.GetRequiredService<PongGame>();
        pong.Startup();

        // Run the game loop
        while (!cancellationToken.IsCancellationRequested)
        {
            // Update the times
            gameTime.TotalTime = totalGameTimer.Elapsed;
            gameTime.DeltaTime = loopTime.Elapsed;
            loopTime.Restart();

            // Iterate the gameloop
            List<PixelBuffer> frame = Loop(pong, gameTime);

            // Render the resulting pixels
            renderer.SetFrame(frame);

            // Calculate how much to sleep to hit our targeted FPS
            int sleepTimeMs = -1;
            if (configuration.TargetGameLoopFPS != -1)
            {
                sleepTimeMs = Math.Max(1, (int)(1000.0 / configuration.TargetGameLoopFPS - loopTime.Elapsed.TotalMilliseconds));
            }
            //Stats
            stats.Frames++;
            if (statsPrinterStopwatch.ElapsedMilliseconds > 1000)
            {
                stats.SleepTime = sleepTimeMs;
                stats.Time = gameTime;
                logger.LogInformation("Gameloop: {@stats}", stats);
                stats = new GameLoopStats();
                statsPrinterStopwatch.Restart();
            }

            // Sleep to hit our targeted FPS
            if (configuration.TargetGameLoopFPS != -1)
            {
                Thread.Sleep(sleepTimeMs);
            }
        }
    }

    private void RenderBlackTestImage(CancellationToken cancellationToken)
    {
        List<PixelBuffer> testFrame = testFraneGenerator.GenerateBlackFrame();
        renderer.SetFrame(testFrame);
        while (!cancellationToken.IsCancellationRequested)
        {
            // Render the test frame
            renderer.PrintAndResetStats();
            Thread.Sleep(50);
        }
    }

    private void RenderStillTestImage(CancellationToken cancellationToken)
    {
        List<PixelBuffer> testFrame = testFraneGenerator.Generate(new GameTime()
        {
            TotalTime = TimeSpan.FromSeconds(configuration.TestImageOffset),
        });
        renderer.SetFrame(testFrame);
        while (!cancellationToken.IsCancellationRequested)
        {
            // Render the test frame
            renderer.PrintAndResetStats();
            Thread.Sleep(50);
        }
    }

    private void RenderMovingTestImage(CancellationToken cancellationToken)
    {
        Stopwatch loopTime = new();
        Stopwatch totalGameTimer = new();
        totalGameTimer.Start();
        GameTime gameTime = new();

        // Run the game loop
        while (!cancellationToken.IsCancellationRequested)
        {
            // Update the times
            gameTime.TotalTime = totalGameTimer.Elapsed;
            gameTime.DeltaTime = loopTime.Elapsed;
            loopTime.Restart();

            // Iterate the gameloop
            List<PixelBuffer> frame = testFraneGenerator.Generate(gameTime);

            // Render the resulting pixels
            renderer.SetFrame(frame);

            // Calculate how much to sleep to hit our targeted FPS
            int sleepTimeMs = Math.Max(1, (int)(1000.0 / configuration.TargetGameLoopFPS - loopTime.Elapsed.TotalMilliseconds));

            //Stats
            stats.Frames++;
            if (statsPrinterStopwatch.ElapsedMilliseconds > 1000)
            {
                stats.SleepTime = sleepTimeMs;
                stats.Time = gameTime;
                logger.LogInformation("Gameloop: {@stats}", stats);
                stats = new GameLoopStats();
                statsPrinterStopwatch.Restart();
            }

            // Sleep to hit our targeted FPS
            Thread.Sleep(sleepTimeMs);
        }
    }

    public List<PixelBuffer> Loop(PongGame pong, GameTime time)
    {
        return pong.Loop(time);
    }
}
