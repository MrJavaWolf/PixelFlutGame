// Official wiki: https://labitat.dk/wiki/Pixelflut 
// DO NOT TRUST THE PROTOCOL DOCUMENTATION: https://github.com/JanKlopper/pixelvloed/blob/master/protocol.md
// Only trust the server code: https://github.com/JanKlopper/pixelvloed/blob/master/C/Server/main.c 
// The server: https://github.com/JanKlopper/pixelvloed
// A example client: https://github.com/Hafpaf/pixelVloedClient 

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace PixelFlut.Core
{
    public class PixelFlutScreenRendererConfiguration
    {

        /// <summary>
        /// The IP to send the bytes to
        /// </summary>
        public string Ip { get; set; } = null!;

        /// <summary>
        ///  The Port to send the bytes to
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Set the on screen offset if you want the game to not run from 0,0
        /// </summary>
        public int OffsetX { get; set; }

        /// <summary>
        /// Set the on screen offset if you want the game to not run from 0,0
        /// </summary>
        public int OffsetY { get; set; }

        /// <summary>
        /// The size of the game area
        /// </summary>
        public int ResultionX { get; set; }

        /// <summary>
        /// The size of the game area
        /// </summary>
        public int ResultionY { get; set; }

        /// <summary>
        /// If the game should be scaled, default 1 (no scale)
        /// </summary>
        public int ScaleX { get; set; }

        /// <summary>
        /// If the game should be scaled, default 1 (no scale)
        /// </summary>
        public int ScaleY { get; set; }

        /// <summary>
        /// Doing rendering it will select a buffer to render
        /// This is how many of those buffers to prepare for each frame
        /// As a rule of thumb:
        /// [NumberOfPreparedBuffers] >= [Number of pixels per frame] / [number of pixels per buffer]
        /// </summary>
        public int NumberOfPreparedBuffers { get; set; }
    }

    public class PixelFlutScreenStats
    {
        /// <summary>
        /// How many bytes is sent
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// How many buffers is sent
        /// </summary>
        public long BuffersSent { get; set; }

        /// <summary>
        /// How many unique buffers that have been sent
        /// </summary>
        public long BuffersPrepared { get; set; }

        /// <summary>
        /// The number of frames recived from the game loop
        /// </summary>
        public long Frames { get; set; }

        /// <summary>
        /// How many pixels the gameloop have produced to be rendered
        /// </summary>
        public long NumberOfPixelsToDraw { get; set; }

        /// <summary>
        /// Due to we select the pixels randomly, the same pixel may be drawn multiple times
        /// </summary>
        public long NumberOfPixelsDrawn { get; set; }
    }

    public class PixelFlutScreenRenderer
    {
        // Overall
        private readonly PixelFlutScreenRendererConfiguration configuration;
        private readonly ILogger<PixelFlutScreenRenderer> logger;

        // Connection
        private Socket socket;
        private IPEndPoint endPoint;

        // Stats
        private Stopwatch stopwatch = new();
        private PixelFlutScreenStats stats = new();


        // Buffers used for sending the bytes
        private readonly List<byte[]> preparedBuffers = new();


        public PixelFlutScreenRenderer(PixelFlutScreenRendererConfiguration configuration, ILogger<PixelFlutScreenRenderer> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);

            // Connnection
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
            endPoint = new IPEndPoint(serverAddr, configuration.Port);

            // Prepare buffers
            for (int i = 0; i < configuration.NumberOfPreparedBuffers; i++) preparedBuffers.Add(PixelFlutScreenProtocol1.CreateBuffer());

            // Stats counter
            stopwatch.Start();
        }

        public void PrepareRender(int numberOfPixelsInFrame, List<PixelFlutPixel> frame)
        {
            // Update stats
            stats.Frames++;
            stats.NumberOfPixelsToDraw += frame.Count;

            // Prepares the pixels
            IEnumerable<PixelFlutPixel> scaledFrameToDraw = ScalePixels(frame);

            // Selects the pixels to render
            for (int i = 0; i < preparedBuffers.Count; i++)
            {
                IEnumerable<PixelFlutPixel> pixelsToDraw = PickRandomPixels(
                    scaledFrameToDraw,
                    numberOfPixelsInFrame,
                    PixelFlutScreenProtocol1.MaximumNumberOfPixel);
                // Prepares the buffer to send
                int pixelNumber = 0;
                foreach (PixelFlutPixel pixel in pixelsToDraw)
                {
                    PixelFlutScreenProtocol1.WriteToBuffer(
                        preparedBuffers[i],
                        pixelNumber,
                        (int)pixel.X + configuration.OffsetX,
                        (int)pixel.Y + configuration.OffsetY,
                        pixel.R,
                        pixel.G,
                        pixel.B,
                        pixel.A);
                    pixelNumber++;
                }
                stats.NumberOfPixelsDrawn += pixelNumber;
                stats.BuffersPrepared++;
            }

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                logger.LogInformation("Screen: {@stats}", stats);
                stats = new PixelFlutScreenStats();
                stopwatch.Restart();
            }
        }

        public void Render()
        {
            byte[] sendBuffer = preparedBuffers[Random.Shared.Next(preparedBuffers.Count)];
            int bytesSent = socket.SendTo(sendBuffer, endPoint);
            stats.BytesSent += bytesSent;
            stats.BuffersSent++;
        }

        private IEnumerable<PixelFlutPixel> ScalePixels(List<PixelFlutPixel> pixels)
        {
            if (configuration.ScaleY == 1 && configuration.ScaleX == 1) return pixels;
            List<PixelFlutPixel> scaledPixel = new();
            foreach (PixelFlutPixel pixel in pixels)
            {
                for (int y = 0; y < configuration.ScaleY; y++)
                {
                    for (int x = 0; x < configuration.ScaleX; x++)
                    {
                        scaledPixel.Add(pixel with
                        {
                            X = pixel.X * configuration.ScaleX + x,
                            Y = pixel.Y * configuration.ScaleY + y,
                        });
                    }
                }
            }
            return scaledPixel;
        }

        private IEnumerable<PixelFlutPixel> PickRandomPixels(
            IEnumerable<PixelFlutPixel> pixels,
            int numberOfPixelsInFrame,
            int amount)
        {
            List<PixelFlutPixel> randomised = new();
            int totalAmountOfPixels = Math.Min(numberOfPixelsInFrame, pixels.Count());
            for (int i = 0; i < amount && i < totalAmountOfPixels; i++)
            {
                randomised.Add(pixels.ElementAt(Random.Shared.Next(totalAmountOfPixels)));
            }
            return randomised;
        }
    }
}


