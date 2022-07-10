using PixelFlut.Core;
using System.Diagnostics;

namespace PixelFlut.TestImage
{
    public class BlackTestImage : IGame
    {
        private readonly ILogger<BlackTestImage> logger;
        private readonly PixelBufferFactory bufferFactory;
        private List<PixelBuffer> frame;

        public BlackTestImage(
            ILogger<BlackTestImage> logger,
            PixelBufferFactory bufferFactory)
        {
            this.logger = logger;
            this.bufferFactory = bufferFactory;

            // Creates the pixel buffer
            this.logger.LogInformation($"Creates pixel buffer for the {this.GetType().Name}...");
            PixelBuffer buffer = bufferFactory.CreateFullScreen();
            frame = new List<PixelBuffer>() { buffer };
            this.logger.LogInformation($"Pixel buffer for the {this.GetType().Name} is ready");

            // Initializes the test image
            DrawBlackTestImage(buffer);

        }

        /// <summary>
        /// Is called onces a frame
        /// </summary>
        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            return frame;
        }

        private void DrawBlackTestImage(PixelBuffer buffer)
        {
            int pixelNumber = 0;
            for (int y = 0; y < bufferFactory.Screen.ResolutionY; y++)
            {
                for (int x = 0; x < bufferFactory.Screen.ResolutionX; x++)
                {
                    // RGB = Black = 0
                    buffer.SetPixel(pixelNumber, x, y, 0, 0, 0, 255);
                    pixelNumber++;
                }
            }
        }
    }
}
