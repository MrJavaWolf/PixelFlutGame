using System.Diagnostics;

namespace pixelflut
{
    public class GameLoop
    {
        private const double targetFPS = 60;
        private readonly PixelFlutRenderer renderer;
        private readonly PixelFlutGamepad gamepad;
        private PixelflutPingPong pingPong;
        public GameLoop(PixelFlutRenderer renderer, PixelFlutGamepad gamepad)
        {
            this.renderer = renderer;
            this.gamepad = gamepad;
            pingPong = new PixelflutPingPong(gamepad);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            while (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Start();
                List<PixelFlutPixel> pixels = Loop();
                renderer.Render(pixels);
                int sleepTimeMs = Math.Max(1, (int)(1000.0 / targetFPS - stopwatch.Elapsed.TotalMilliseconds));
                await Task.Delay(sleepTimeMs, cancellationToken);
                stopwatch.Reset();
            }
        }

        public List<PixelFlutPixel> Loop()
        {
            return pingPong.Loop();
        }
    }
}
