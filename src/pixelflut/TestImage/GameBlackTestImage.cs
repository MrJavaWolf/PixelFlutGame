using PixelFlut.Core;
using System.Diagnostics;

namespace PixelFlut.TestImage
{
    public class GameBlackTestImage : IGame
    {
        private readonly ILogger<GameBlackTestImage> logger;
        private readonly PixelBufferFactory bufferFactory;
        private List<PixelBuffer> frame;

        public GameBlackTestImage(
            ILogger<GameBlackTestImage> logger,
            PixelBufferFactory bufferFactory)
        {
            this.logger = logger;
            this.bufferFactory = bufferFactory;

            // Creates the pixel buffer
            Stopwatch sw = Stopwatch.StartNew();
            this.logger.LogInformation("Creates pixel buffer for the test image...");
            PixelBuffer buffer = bufferFactory.CreateFullScreen();
            frame = new List<PixelBuffer>() { buffer };
            this.logger.LogInformation($"Pixel buffer for the test image is ready, time took to create: {sw.ElapsedMilliseconds} ms");

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
            for (int y = 0; y < bufferFactory.Screen.ResultionY; y++)
            {
                for (int x = 0; x < bufferFactory.Screen.ResultionX; x++)
                {
                    // RGB = Black = 0
                    buffer.SetPixel(pixelNumber, x, y, 0, 0, 0, 255);
                    pixelNumber++;
                }
            }
        }
    }
}
