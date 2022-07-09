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
    /// Which game to play
    /// </summary>
    public string GameToPlay { get; set; } = "";
}

public class GameLoopStats
{
    public GameTime Time { get; set; } = new();
    public long SleepTime { get; set; }
    public long Frames { get; set; }
}

public class GameLoop
{
    // Generel
    private readonly ILogger<GameLoop> logger;
    private readonly PixelFlutScreen renderer;
    private readonly IServiceProvider provider;
    private readonly GameLoopConfiguration configuration;
    private readonly GamePadsController gamePadsController;

    // Stats
    private GameLoopStats stats = new();
    private Stopwatch statsPrinterStopwatch = new();

    public GameLoop(
        ILogger<GameLoop> logger,
        PixelFlutScreen renderer,
        IServiceProvider provider,
        GamePadsController gamePadsController,
        GameLoopConfiguration configuration)
    {
        this.logger = logger;
        this.renderer = renderer;
        this.provider = provider;
        this.gamePadsController = gamePadsController;
        this.configuration = configuration;
        logger.LogInformation($"GameLoop: {{@configuration}}", configuration);
        statsPrinterStopwatch.Start();
    }

    public void Run(CancellationToken cancellationToken)
    {
        // Start the renderer
        renderer.StartRenderThreads(cancellationToken);
        RunGameLoop(cancellationToken);
    }

    private void RunGameLoop(CancellationToken cancellationToken)
    {
        // Setup the timers
        Stopwatch loopTime = new();
        Stopwatch totalGameTimer = new();
        GameTime gameTime = new();
        totalGameTimer.Start();
        gamePadsController.Update();

        // Setup the game
        IGame gameSelector = provider.GetRequiredService<GameSelector>();

        // Run the game loop
        while (!cancellationToken.IsCancellationRequested)
        {
            // Update the times
            gameTime.TotalTime = totalGameTimer.Elapsed;
            gameTime.DeltaTime = loopTime.Elapsed;
            loopTime.Restart();

            // Iterate the gameloop
            gamePadsController.Update();
            List<PixelBuffer> frame = gameSelector.Loop(gameTime, gamePadsController.GamePads);

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
}
