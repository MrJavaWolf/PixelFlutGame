using PixelFlut.Core;
using System.Drawing;

namespace PixelFlut.TestImage
{
    public class RainbowTestImage : IGame
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
        private readonly ILogger<RainbowTestImage> logger;
        private readonly PixelBufferFactory bufferFactory;
        private List<PixelBuffer> frame;

        public RainbowTestImage(
            Configuration config,
            ILogger<RainbowTestImage> logger,
            PixelBufferFactory bufferFactory)
        {
            this.config = config;
            this.logger = logger;
            this.bufferFactory = bufferFactory;

            // Creates the pixel buffer
            this.logger.LogInformation($"Creates pixel buffer for the {this.GetType().Name}...");
            PixelBuffer buffer = bufferFactory.CreateFullScreen();
            frame = new List<PixelBuffer>() { buffer };
            this.logger.LogInformation($"Pixel buffer for the {this.GetType().Name} is ready");

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
            for (int y = 0; y < bufferFactory.Screen.ResolutionY; y++)
            {
                for (int x = 0; x < bufferFactory.Screen.ResolutionX; x++)
                {
                    double hue = (x + y + time.TotalTime.TotalSeconds * 100) * 0.3 % 360;
                    Color c = MathHelper.ColorFromHSV(hue, 1, 1);
                    buffer.SetPixel(pixelNumber, x, y, c.R, c.G, c.B, 255);
                    pixelNumber++;
                }
            }
        }
    }
}
