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
        /// The Port of the Pixelflut
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
        /// How many of send buffers will be prepared for each frame
        /// Doing rendering it will select a buffer and send it the pixelflut
        /// Generally the more buffers, the better picture quiality (bigger pixel coverage), 
        /// but at the cost of more CPU usage
        /// 
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
        /// How many buffers is sent
        /// </summary>
        public long TotalBuffersSent { get; set; }

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

            long framePixelNumber = 0;

            for (int i = 0; i < preparedBuffers.Count; i++)
            {
                for (int pixelNumber = 0; pixelNumber < screenProtocol.PixelsPerBuffer; pixelNumber++)
                {
                    // Selects the pixels to render
                    PixelFlutPixel randomPixel = PickNextPixel(
                        frame,
                        numberOfPixelsInFrame,
                        framePixelNumber);

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
                    framePixelNumber++;
                }

                // Update stats
                stats.NumberOfPixelsDrawn += screenProtocol.PixelsPerBuffer;
                stats.BuffersPrepared++;
            }

            PrintAndResetStats();
        }

        public void PrintAndResetStats()
        {
            if (statsPrinterStopwatch.ElapsedMilliseconds > 1000)
            {
                logger.LogInformation("Screen: {@stats}", stats);
                long totalBuffers = stats.TotalBuffersSent;
                stats = new PixelFlutScreenStats();
                stats.TotalBuffersSent += totalBuffers;
                statsPrinterStopwatch.Restart();
            }
        }

        private PixelFlutPixel PickRandomPixel(
            IEnumerable<PixelFlutPixel> frame,
            int numberOfPixelsInFrame)
        {
            int totalAmountOfPixels = Math.Min(numberOfPixelsInFrame, frame.Count());
            return frame.ElementAt(Random.Shared.Next(totalAmountOfPixels));
        }

        private PixelFlutPixel PickNextPixel(
           IEnumerable<PixelFlutPixel> frame,
           int numberOfPixelsInFrame,
           long framePixelNumber)
        {
            int index = (int)(framePixelNumber % numberOfPixelsInFrame);
            return frame.ElementAt(index);
        }

        public void Render()
        {
            // Pick a buffer to render
            byte[] sendBuffer = SelectNextBuffer();

            // Send 
            int bytesSent = socket.SendTo(sendBuffer, endPoint);

            // Update stats
            stats.BytesSent += bytesSent;
            stats.BuffersSent++;
            stats.TotalBuffersSent++;

            // Wait if requested, usefull on slower single core CPU's
            if (configuration.SleepTimeBetweenFrames != -1)
            {
                Thread.Sleep(configuration.SleepTimeBetweenFrames);
            }
        }

        private byte[] SelectRandomBuffer()
        {
            byte[] sendBuffer = preparedBuffers[Random.Shared.Next(preparedBuffers.Count)];
            return sendBuffer;
        }

        private byte[] SelectNextBuffer()
        {
            byte[] sendBuffer = preparedBuffers[(int)(stats.TotalBuffersSent % preparedBuffers.Count)];
            return sendBuffer;
        }
    }
}


