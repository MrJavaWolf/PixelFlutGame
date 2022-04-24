using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace pixelflut
{
    public class GameLoopConfiguration
    {
        public double TargetGameLoopUpdateSpeed = 60;
    }

    public class GameLoop
    {
        private readonly IServiceProvider provider;
        private readonly GameLoopConfiguration configuration;
        private List<PixelFlutPixel> pixels = new();

        public GameLoop(IServiceProvider provider, GameLoopConfiguration configuration)
        {
            this.provider = provider;
            this.configuration = configuration;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            new Thread(() => RendererThread(cancellationToken)).Start();
            
            Stopwatch stopwatch = new Stopwatch();
            PixelflutPingPong pingpong = provider.GetRequiredService<PixelflutPingPong>();
            while (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Start();
                pixels = Loop(pingpong);
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

        public List<PixelFlutPixel> Loop(PixelflutPingPong pingpong)
        {
            return pingpong.Loop();
        }
    }
}
