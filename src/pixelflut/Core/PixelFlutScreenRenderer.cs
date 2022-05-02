using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace PixelFlut.Core
{
    public class PixelFlutScreenRendererConfiguration
    {

        /// <summary>
        /// The IP of the Pixelflut
        /// </summary>
        public string Ip { get; set; } = null!;

        /// <summary>
        ///  The Port of the Pixelflut
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Set the on screen offset if you want the game to not run from 0,0 (usually top left)
        /// </summary>
        public int OffsetX { get; set; }

        /// <summary>
        /// Set the on screen offset if you want the game to not run from 0,0 (usually top left)
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
        /// Doing rendering it will select a buffer to render
        /// This is how many of those buffers to prepare for each frame
        /// As a rule of thumb:
        /// [NumberOfPreparedBuffers] >= [Number of pixels per frame] / [number of pixels per buffer]
        /// </summary>
        public int NumberOfPreparedBuffers { get; set; }

        /// <summary>
        /// Sleep time between frames, set to -1 to run 100% CPU
        /// </summary>
        public int SleepTimeBetweenFrames { get; set; }
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
        // Generel
        private readonly IPixelFlutScreenProtocol screenProtocol;
        private readonly PixelFlutScreenRendererConfiguration configuration;
        private readonly ILogger<PixelFlutScreenRenderer> logger;

        // Connection
        private Socket socket;
        private IPEndPoint endPoint;

        // Stats
        private Stopwatch statsPrinterStopwatch = new();
        private PixelFlutScreenStats stats = new();


        // Buffers used for sending the bytes
        private readonly List<byte[]> preparedBuffers = new();


        public PixelFlutScreenRenderer(
            IPixelFlutScreenProtocol screenProtocol,
            PixelFlutScreenRendererConfiguration configuration,
            ILogger<PixelFlutScreenRenderer> logger)
        {
            this.screenProtocol = screenProtocol;
            this.configuration = configuration;
            this.logger = logger;
            logger.LogInformation($"PixelFlutScreen: {{@pixelFlutScreen}}", configuration);

            // Setup connnection
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
            endPoint = new IPEndPoint(serverAddr, configuration.Port);

            // Prepare buffers
            for (int i = 0; i < configuration.NumberOfPreparedBuffers; i++)
                preparedBuffers.Add(screenProtocol.CreateBuffer());

            // Stats counter
            statsPrinterStopwatch.Start();
        }

        public void PrepareRender(int numberOfPixelsInFrame, List<PixelFlutPixel> frame)
        {
            // Update stats
            stats.Frames++;
            stats.NumberOfPixelsToDraw += frame.Count;

            for (int i = 0; i < preparedBuffers.Count; i++)
            {
                for (int pixelNumber = 0; pixelNumber < screenProtocol.PixelsPerBuffer; i++)
                {
                    // Selects the pixels to render
                    PixelFlutPixel randomPixel = PickRandomPixel(
                        frame,
                        numberOfPixelsInFrame);

                    // Write the pixel to the buffer
                    screenProtocol.WriteToBuffer(
                        preparedBuffers[i],
                        pixelNumber,
                        (int)randomPixel.X + configuration.OffsetX,
                        (int)randomPixel.Y + configuration.OffsetY,
                        randomPixel.R,
                        randomPixel.G,
                        randomPixel.B,
                        randomPixel.A);
                }

                // Update stats
                stats.NumberOfPixelsDrawn += screenProtocol.PixelsPerBuffer;
                stats.BuffersPrepared++;
            }

            if (statsPrinterStopwatch.ElapsedMilliseconds > 1000)
            {
                logger.LogInformation("Screen: {@stats}", stats);
                stats = new PixelFlutScreenStats();
                statsPrinterStopwatch.Restart();
            }
        }

        public void Render()
        {
            // Pick a buffer to render
            byte[] sendBuffer = preparedBuffers[Random.Shared.Next(preparedBuffers.Count)];
            
            // Send 
            int bytesSent = socket.SendTo(sendBuffer, endPoint);
            
            // Update stats
            stats.BytesSent += bytesSent;
            stats.BuffersSent++;

            // Wait if requested, usefull on slower single core CPU's
            if (configuration.SleepTimeBetweenFrames != -1)
            {
                Thread.Sleep(configuration.SleepTimeBetweenFrames);
            }
        }

        private PixelFlutPixel PickRandomPixel(
           IEnumerable<PixelFlutPixel> frame,
           int numberOfPixelsInFrame)
        {
            List<PixelFlutPixel> randomised = new();
            int totalAmountOfPixels = Math.Min(numberOfPixelsInFrame, frame.Count());
            return frame.ElementAt(Random.Shared.Next(totalAmountOfPixels));
        }
    }
}


