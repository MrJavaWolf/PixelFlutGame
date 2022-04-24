using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace pixelflut
{
    public class GameLoopConfiguration
    {
        public double TargetGameLoopUpdateSpeed { get; set; } = 60;
        public int NumberOfRenderer { get; set; } = 1;
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

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < configuration.NumberOfRenderer; i++)
            {
                new Thread(() => RendererThread(cancellationToken)).Start();
            }

            Stopwatch stopwatch = new Stopwatch();
            Stopwatch timer = new Stopwatch();
            timer.Start();
            PixelflutPingPong pingpong = provider.GetRequiredService<PixelflutPingPong>();
            pingpong.Startup();
            while (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Start();
                pixels = Loop(pingpong, timer.Elapsed).ToList();
                int sleepTimeMs = Math.Max(1, (int)(1000.0 / configuration.TargetGameLoopUpdateSpeed - stopwatch.Elapsed.TotalMilliseconds));
                await Task.Delay(sleepTimeMs, cancellationToken);
                stopwatch.Reset();
            }
        }

        private void RendererThread(CancellationToken cancellationToken)
        {
            PixelFlutRenderer renderer = provider.GetRequiredService<PixelFlutRenderer>();
            while (!cancellationToken.IsCancellationRequested)
            {
                renderer.Render(pixels);
            }
        }

        public List<PixelFlutPixel> Loop(PixelflutPingPong pingpong, TimeSpan time)
        {
            return pingpong.Loop(time);
        }
    }
}
