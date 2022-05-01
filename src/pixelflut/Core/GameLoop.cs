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

        public GameLoop(ILogger<GameLoop> logger, IServiceProvider provider, GameLoopConfiguration configuration)
        {
            this.provider = provider;
            this.configuration = configuration;
            logger.LogInformation($"GameLoop: {{@configuration}}", configuration);
        }

        public void Run(CancellationToken cancellationToken)
        {
            List<PixelFlutScreenRenderer> renderers = new List<PixelFlutScreenRenderer>();
            for (int i = 0; i < configuration.NumberOfRenderer; i++)
            {
                PixelFlutScreenRenderer renderer = provider.GetRequiredService<PixelFlutScreenRenderer>();
                renderers.Add(renderer);
                Thread t = new(() => RendererThread(renderer, cancellationToken));
                t.Priority = ThreadPriority.Highest;
                t.Start();
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
                List<PixelFlutPixel> pixels = Loop(pingpong, gameTime).ToList();
                foreach (var renderer in renderers)
                    renderer.PrepareRender(pixels);
                int sleepTimeMs = Math.Max(1, (int)(1000.0 / configuration.TargetGameLoopFPS - loopTime.Elapsed.TotalMilliseconds));
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

        public List<PixelFlutPixel> Loop(PingPongGame pingpong, GameTime time)
        {
            return pingpong.Loop(time);
        }
    }
}
