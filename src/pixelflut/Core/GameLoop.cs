using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixelFlut.PingPong;
using System.Diagnostics;

namespace PixelFlut.Core
{
    public class GameLoopConfiguration
    {
        public double TargetGameLoopFPS { get; set; }
        public int NumberOfRenderer { get; set; }
    }

    public class GameLoop
    {
        private readonly IServiceProvider provider;
        private readonly GameLoopConfiguration configuration;
        private List<PixelFlutPixel> pixels = new();

        public GameLoop(ILogger<GameLoop> logger, IServiceProvider provider, GameLoopConfiguration configuration)
        {
            this.provider = provider;
            this.configuration = configuration;
            logger.LogInformation($"GameLoop: {{@configuration}}", configuration);
        }

        public void Run(CancellationToken cancellationToken)
        {
            for (int i = 0; i < configuration.NumberOfRenderer; i++)
            {
                new Thread(() => RendererThread(cancellationToken)).Start();
            }

            Stopwatch loopTime = new();
            Stopwatch totalGameTimer = new();
            GameTime gameTime = new();
            totalGameTimer.Start();
            PingPongGame pingpong = provider.GetRequiredService<PingPongGame>();
            pingpong.Startup();
            while (!cancellationToken.IsCancellationRequested)
            {
                gameTime.TotalTime = totalGameTimer.Elapsed;
                gameTime.DeltaTime = loopTime.Elapsed;
                loopTime.Restart();
                pixels = Loop(pingpong, gameTime).ToList();
                int sleepTimeMs = Math.Max(1, (int)(1000.0 / configuration.TargetGameLoopFPS - loopTime.Elapsed.TotalMilliseconds));
                Thread.Sleep(sleepTimeMs);
            }
        }

        private void RendererThread(CancellationToken cancellationToken)
        {
            PixelFlutScreenRenderer renderer = provider.GetRequiredService<PixelFlutScreenRenderer>();
            while (!cancellationToken.IsCancellationRequested)
            {
                renderer.Render(pixels);
            }
        }

        public List<PixelFlutPixel> Loop(PingPongGame pingpong, GameTime time)
        {
            return pingpong.Loop(time);
        }
    }
}
