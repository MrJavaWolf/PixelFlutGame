// Official wiki: https://labitat.dk/wiki/Pixelflut 
// DO NOT TRUST THE PROTOCOL DOCUMENTATION: https://github.com/JanKlopper/pixelvloed/blob/master/protocol.md
// Only trust the server code: https://github.com/JanKlopper/pixelvloed/blob/master/C/Server/main.c 
// The server: https://github.com/JanKlopper/pixelvloed
// A example client: https://github.com/Hafpaf/pixelVloedClient 

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace pixelflut
{
    public class PixelFlutRendererConfiguration
    {
        public string Ip { get; set; } = "10.42.1.12";
        public int Port { get; set; } = 5005;

        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;

        public int ResultionX { get; set; } = 500;
        public int ResultionY { get; set; } = 500;

        public int ScaleX { get; set; } = 1;
        public int ScaleY { get; set; } = 1;
    }

    public class PixelFlutRenderer
    {
        private readonly PixelFlutRendererConfiguration configuration;
        private Socket socket;
        private IPEndPoint endPoint;

        public PixelFlutRenderer(PixelFlutRendererConfiguration configuration, ILogger<PixelFlutRenderer> logger)
        {
            this.configuration = configuration;
            logger.LogInformation("PixelFlutScreen: {{@pixelFlutScreen}}", configuration);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
            endPoint = new IPEndPoint(serverAddr, configuration.Port);
        }

        public void Render(List<PixelFlutPixel> pixels)
        {
            // Inefficient to create new buffer every frame
            byte[] send_buffer = PixelFlutScreenProtocol1.CreateBuffer();
            IEnumerable<PixelFlutPixel> scaledPixelsToDraw = ScalePixels(pixels);
            IEnumerable<PixelFlutPixel> pixelsToDraw = PickRandomPixels(scaledPixelsToDraw, PixelFlutScreenProtocol1.MaximumNumberOfPixel);
            int pixelNumber = 0;
            foreach (PixelFlutPixel pixel in pixelsToDraw)
            {
                PixelFlutScreenProtocol1.WriteToBuffer(send_buffer, pixelNumber, (int)pixel.X, (int)pixel.Y, pixel.R, pixel.G, pixel.B, pixel.A);
                pixelNumber++;
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
                        scaledPixel.Add(pixel with { 
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
            foreach (PixelFlutPixel pixel in pixels)
            {
                if (randomised.Count > 0)
                {
                    randomised.Insert(Random.Shared.Next(randomised.Count + 1), pixel);
                }
                else
                {
                    randomised.Add(pixel);
                }
            }
            return randomised.Take(amount);
        }
    }
}
