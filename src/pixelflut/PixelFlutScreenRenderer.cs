// Official wiki: https://labitat.dk/wiki/Pixelflut 
// DO NOT TRUST THE PROTOCOL DOCUMENTATION: https://github.com/JanKlopper/pixelvloed/blob/master/protocol.md
// Only trust the server code: https://github.com/JanKlopper/pixelvloed/blob/master/C/Server/main.c 
// The server: https://github.com/JanKlopper/pixelvloed
// A example client: https://github.com/Hafpaf/pixelVloedClient 

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace PixelFlut
{
    public class PixelFlutRendererConfiguration
    {
        public string Ip { get; set; } = null!;
        public int Port { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public int ResultionX { get; set; }
        public int ResultionY { get; set; }
        public int ScaleX { get; set; }
        public int ScaleY { get; set; }
    }

    public class PixelFlutScreenRenderer
    {
        private readonly PixelFlutRendererConfiguration configuration;
        private Socket socket;
        private IPEndPoint endPoint;
        private List<PixelFlutPixel>? lastRenderedPixels;
        private int samePixelsCounter;
        private readonly byte[] send_buffer;
        public PixelFlutScreenRenderer(PixelFlutRendererConfiguration configuration, ILogger<PixelFlutScreenRenderer> logger)
        {
            this.configuration = configuration;
            logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
            endPoint = new IPEndPoint(serverAddr, configuration.Port);
            send_buffer = PixelFlutScreenProtocol1.CreateBuffer();
        }

        public void Render(List<PixelFlutPixel> pixels)
        {
            if (lastRenderedPixels != pixels || samePixelsCounter % 10 == 0)
            {
                lastRenderedPixels = pixels;
                samePixelsCounter = 1;
                IEnumerable<PixelFlutPixel> scaledPixelsToDraw = ScalePixels(pixels);
                IEnumerable<PixelFlutPixel> pixelsToDraw = PickRandomPixels(scaledPixelsToDraw, PixelFlutScreenProtocol1.MaximumNumberOfPixel);
                int pixelNumber = 0;
                foreach (PixelFlutPixel pixel in pixelsToDraw)
                {
                    PixelFlutScreenProtocol1.WriteToBuffer(send_buffer, pixelNumber, (int)pixel.X, (int)pixel.Y, pixel.R, pixel.G, pixel.B, pixel.A);
                    pixelNumber++;
                }
            }
            else
            {
                samePixelsCounter++;
            }
            socket.SendTo(send_buffer, endPoint);
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
