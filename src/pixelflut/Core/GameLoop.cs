using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixelFlut.Pong;
using System.Diagnostics;

namespace PixelFlut.Core
{
    public class GameLoopConfiguration
    {
        /// <summary>
        /// How many time a second the game loop will run
        /// </summary>
        public double TargetGameLoopFPS { get; set; }

        /// <summary>
        /// How many renderer threads there will be created
        /// </summary>
        public int NumberOfRenderer { get; set; }

        /// <summary>
        /// Whether it should render a test image insteed of playing a game
        /// </summary>
        public bool EnableTestImage { get; set; }

        /// <summary>
        /// The test image offset
        /// </summary>
        public double TestImageOffset { get; set; }
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
        private readonly IServiceProvider provider;
        private readonly PixelFlutScreenRendererConfiguration screenConfiguration;
        private readonly GameLoopConfiguration configuration;

        // Stats
        private GameLoopStats stats = new();
        private Stopwatch statsPrinterStopwatch = new();

        public GameLoop(
            ILogger<GameLoop> logger,
            IServiceProvider provider,
            PixelFlutScreenRendererConfiguration screenConfiguration,
            GameLoopConfiguration configuration)
        {
            this.logger = logger;
            this.provider = provider;
            this.screenConfiguration = screenConfiguration;
            this.configuration = configuration;
            logger.LogInformation($"GameLoop: {{@configuration}}", configuration);
            statsPrinterStopwatch.Start();
        }

        public void Run(CancellationToken cancellationToken)
        {
            // Start the renderers
            List<PixelFlutScreenRenderer> renderers = new List<PixelFlutScreenRenderer>();
            for (int i = 0; i < configuration.NumberOfRenderer; i++)
            {
                PixelFlutScreenRenderer renderer = provider.GetRequiredService<PixelFlutScreenRenderer>();
                renderers.Add(renderer);
                Thread t = new(() => RendererThread(renderer, cancellationToken));
                // t.Priority = ThreadPriority.Highest;
                t.Start();
            }

            // Setup the timers
            Stopwatch loopTime = new();
            Stopwatch totalGameTimer = new();
            GameTime gameTime = new();
            totalGameTimer.Start();

            if (configuration.EnableTestImage)
            {
                var testImage = TestImageGenerator.Generate(new GameTime()
                {
                    TotalTime = TimeSpan.FromSeconds(configuration.TestImageOffset),
                }, screenConfiguration);
                // Render the test frame
                foreach (var renderer in renderers)
                    renderer.PrepareRender(testImage.numberOfPixels, testImage.frame);
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                }
                return;
            }

            // Setup the game
            PongGame pong = provider.GetRequiredService<PongGame>();
            pong.Startup();

            // RUn the game loop
            while (!cancellationToken.IsCancellationRequested)
            {
                // Update the times
                gameTime.TotalTime = totalGameTimer.Elapsed;
                gameTime.DeltaTime = loopTime.Elapsed;
                loopTime.Restart();

                // Iterate the gameloop
                (int numberOfPixels, List<PixelFlutPixel> frame) = Loop(pong, gameTime);

                // Render the resulting pixels
                foreach (var renderer in renderers)
                    renderer.PrepareRender(numberOfPixels, frame);

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

        private void RendererThread(PixelFlutScreenRenderer renderer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                renderer.Render();
            }
        }

        public (int numberOfPixels, List<PixelFlutPixel> frame) Loop(PongGame pong, GameTime time)
        {
            return pong.Loop(time);
        }


    }
}
