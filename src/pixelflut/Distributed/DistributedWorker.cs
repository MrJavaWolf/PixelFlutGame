using PixelFlut.Core;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PixelFlut.Distributed;

public class DistributedWorkerConfiguration
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public int NumberOfPackages { get; set; } = 10;

    public class DistributedWorker : IGame
    {
        private readonly ILogger<DistributedWorker> logger;
        public DistributedWorkerConfiguration Config { get; }
        public PixelFlutScreen PixelFlutScreen { get; }
        private IPEndPoint endpoint;
        private UdpClient listener;
        private IPixelFlutScreenProtocol pixelFlutScreenProtocol;

        public DistributedWorker(
            DistributedWorkerConfiguration config,
            PixelFlutScreen pixelFlutScreen,
            ILogger<DistributedWorker> logger)
        {
            Config = config;
            PixelFlutScreen = pixelFlutScreen;
            this.logger = logger;
            endpoint = new IPEndPoint(IPAddress.Parse(config.Ip), config.Port);
            listener = new UdpClient(Config.Port);
        }


        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            Task t = Task.Run(async () => await StartListenerAsync(cancellationTokenSource.Token));
            Task.WaitAll(t);
            return new List<PixelBuffer> { };
        }


        private async Task<List<PixelBuffer>> StartListenerAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"{nameof(DistributedWorker)} configuration: {{@config}}", Config);
            try
            {
                await SendRequestAsync(cancellationToken);
                List<PixelBuffer> frame = await ReadResponseFromServerAsync(cancellationToken);
                return frame;
            }
            catch (SocketException e)
            {
                logger.LogError(e, "Udp client failed");
                return new List<PixelBuffer> { };

            }
        }

        private async Task<List<PixelBuffer>> ReadResponseFromServerAsync(CancellationToken cancellationToken)
        {
            List<byte[]> bytes = new List<byte[]>();
            for (int i = 0; i < Config.NumberOfPackages; i++)
            {
                logger.LogInformation("Waiting for response...");
                UdpReceiveResult result = await listener.ReceiveAsync(cancellationToken);

                logger.LogInformation($"Received response from {result.RemoteEndPoint} :");
                logger.LogInformation($" {Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length)}");

                byte[] buffer = new byte[result.Buffer.Length];
                Array.Copy(result.Buffer, buffer, result.Buffer.Length);
                bytes.Add(buffer);
            }
            return new List<PixelBuffer>
            {
                new PixelBuffer(1, bytes)
            };
        }

        private async Task SendRequestAsync(CancellationToken cancellationToken)
        {
            using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] bytes = BitConverter.GetBytes(Config.NumberOfPackages);
            await sock.SendToAsync(bytes, endpoint, cancellationToken);
        }
    }
}
