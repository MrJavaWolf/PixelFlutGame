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
    }

    public class PixelFlutScreenStats
    {
        public long BytesSentToUDPSocket { get; set; }
        public long BuffersSentToUDPSocket { get; set; }
        public long DifferentFrames { get; set; }
        public long DifferentBuffers { get; set; }
    }

    public class PixelFlutScreenRenderer
    {
        private readonly PixelFlutScreenRendererConfiguration configuration;
        private readonly ILogger<PixelFlutScreenRenderer> logger;
        private Socket socket;
        private IPEndPoint endPoint;
        private List<PixelFlutPixel>? lastRenderedPixels;
        private int samePixelsCounter;
        private readonly byte[] send_buffer;
        private PixelFlutScreenStats stats = new();
        private Stopwatch stopwatch = new();

        public PixelFlutScreenRenderer(PixelFlutScreenRendererConfiguration configuration, ILogger<PixelFlutScreenRenderer> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
            endPoint = new IPEndPoint(serverAddr, configuration.Port);
            send_buffer = PixelFlutScreenProtocol1.CreateBuffer();

            stopwatch.Start();
        }

        public void Render(List<PixelFlutPixel> frame)
        {
            if (lastRenderedPixels != frame || samePixelsCounter % 10 == 0)
            {
                if (lastRenderedPixels != frame)
                {
                    stats.DifferentFrames++;
                }

                lastRenderedPixels = frame;
                samePixelsCounter = 1;
                IEnumerable<PixelFlutPixel> scaledPixelsToDraw = ScalePixels(frame);
                IEnumerable<PixelFlutPixel> pixelsToDraw = PickRandomPixels(scaledPixelsToDraw, PixelFlutScreenProtocol1.MaximumNumberOfPixel);
                int pixelNumber = 0;
                foreach (PixelFlutPixel pixel in pixelsToDraw)
                {
                    PixelFlutScreenProtocol1.WriteToBuffer(send_buffer, pixelNumber, (int)pixel.X + configuration.OffsetX, (int)pixel.Y + configuration.OffsetY, pixel.R, pixel.G, pixel.B, pixel.A);
                    pixelNumber++;
                }
                stats.DifferentBuffers++;
            }
            else
            {
                samePixelsCounter++;
            }
            //socket.SendTo(send_buffer, endPoint);
            stats.BytesSentToUDPSocket += send_buffer.Length;
            stats.BuffersSentToUDPSocket++;

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                logger.LogInformation("Screen: {@stats}", stats);
                stats = new PixelFlutScreenStats();
                stopwatch.Restart();
            }
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

        private IEnumerable<PixelFlutPixel> PickRandomPixels(IEnumerable<PixelFlutPixel> pixels, int amount)
        {
            List<PixelFlutPixel> randomised = new();
            int totalAmountOfPixels = pixels.Count(); ;
            for (int i = 0; i < amount && i < totalAmountOfPixels; i++)
            {
                randomised.Add(pixels.ElementAt(Random.Shared.Next(totalAmountOfPixels)));
            }
            return randomised;
        }
    }
}
