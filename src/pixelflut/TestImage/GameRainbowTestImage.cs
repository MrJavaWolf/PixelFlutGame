using PixelFlut.Core;
using System.Diagnostics;

namespace PixelFlut.TestImage
{
    public class GameRainbowTestImage : IGame
    {
        public class Configuration
        {
            /// <summary>
            /// Whether the test image should be moving
            /// </summary>
            public bool Moving { get; set; }

            /// <summary>
            /// Change the still image's offset
            /// </summary>
            public double TestImageOffset { get; set; }
        }

        private readonly Configuration config;
        private readonly ILogger<GameRainbowTestImage> logger;
        private readonly PixelBufferFactory bufferFactory;
        private List<PixelBuffer> frame;

        public GameRainbowTestImage(
            Configuration config,
            ILogger<GameRainbowTestImage> logger,
            PixelBufferFactory bufferFactory)
        {
            this.config = config;
            this.logger = logger;
            this.bufferFactory = bufferFactory;

            // Initialize
            Stopwatch sw = new();
            sw.Start();
            this.logger.LogInformation($"Creates pixel buffer for the {this.GetType().Name}...");
            PixelBuffer buffer = bufferFactory.Create(bufferFactory.Screen.ResultionY * bufferFactory.Screen.ResultionX);
            frame = new List<PixelBuffer>() { buffer };
            this.logger.LogInformation($"Pixel buffer for the {this.GetType().Name} is ready, time took to create: {sw.ElapsedMilliseconds} ms");

            // Initializes the test image
            DrawRainBowTestImage(
                new GameTime() { TotalTime = TimeSpan.FromSeconds(config.TestImageOffset) },
                buffer);
        }

        /// <summary>
        /// Is called onces a frame
        /// </summary>
        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            if (config.Moving)
            {
                DrawRainBowTestImage(time, frame[0]);
            }
            return frame;
        }

        public void DrawRainBowTestImage(GameTime time, PixelBuffer buffer)
        {
            int pixelNumber = 0;
            for (int y = 0; y < bufferFactory.Screen.ResultionY; y++)
            {
                for (int x = 0; x < bufferFactory.Screen.ResultionX; x++)
                {
                    var c = MathHelper.ColorFromHSV(
                        (x + y + time.TotalTime.TotalSeconds * 100) * 0.3 % 360,
                        1,
                        1);
                    buffer.SetPixel(
                        pixelNumber,
                        x,
                        y,
                        c.R,
                        c.G,
                        c.B,
                        255);
                    pixelNumber++;
                }
            }
        }
    }
}
