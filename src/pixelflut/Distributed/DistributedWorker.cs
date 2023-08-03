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
        private readonly IPixelFlutScreenProtocol screenProtocol;
        private readonly ILogger<DistributedWorker> logger;
        public DistributedWorkerConfiguration Config { get; }
        public PixelFlutScreen PixelFlutScreen { get; }
        private IPEndPoint serverEndpoint;
        private IPEndPoint localEndpoint;
        private UdpClient udpClientSender;
        private UdpClient udpClientReciever;
        public const int SIO_UDP_CONNRESET = -1744830452;

        public DistributedWorker(
            IPixelFlutScreenProtocol screenProtocol,
            DistributedWorkerConfiguration config,
            PixelFlutScreen pixelFlutScreen,
            ILogger<DistributedWorker> logger)
        {
            this.screenProtocol = screenProtocol;
            Config = config;
            PixelFlutScreen = pixelFlutScreen;
            this.logger = logger;
            int localPort = Random.Shared.Next(10000, 20000);
            //localEndpoint = new IPEndPoint(IPAddress.Any, localPort);
            localEndpoint = new IPEndPoint(IPAddress.Any, localPort);
            udpClientReciever = new UdpClient();
            udpClientReciever.ExclusiveAddressUse = false;
            udpClientReciever.Client.IOControl(
                (IOControlCode)SIO_UDP_CONNRESET,
                new byte[] { 0, 0, 0, 0 },
                null
            );
            udpClientReciever.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClientReciever.Client.Bind(localEndpoint);

            udpClientSender = new UdpClient();
            udpClientSender.ExclusiveAddressUse = false;
            udpClientSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClientSender.Client.Bind(localEndpoint);


            serverEndpoint = new IPEndPoint(IPAddress.Parse(config.Ip), config.Port);
            logger.LogInformation($"{nameof(DistributedWorker)} configuration: {{@config}}", Config);

        }


        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
            var t = Task.Run(async () => await GetBuffersFromDistributionServer(cancellationTokenSource.Token));
            try
            {
                return t.Result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occoured");
                return new List<PixelBuffer> { };
            }
        }


        private async Task<List<PixelBuffer>> GetBuffersFromDistributionServer(CancellationToken cancellationToken)
        {
            try
            {
                Task t = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await SendRequestAsync(cancellationToken);
                });
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
                logger.LogInformation($"Waiting for response, listing on '{this.localEndpoint}' ...");
                UdpReceiveResult result = await udpClientReciever.ReceiveAsync(cancellationToken);

                logger.LogInformation($"Received response from {result.RemoteEndPoint} - Length: {result.Buffer.Length}");
                logger.LogInformation($"Payload: '{Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length)}'");

                byte[] buffer = new byte[result.Buffer.Length];
                Array.Copy(result.Buffer, buffer, result.Buffer.Length);
                bytes.Add(buffer);
            }
            return new List<PixelBuffer>
            {
                new PixelBuffer(1,screenProtocol, bytes)
            };
        }

        private async Task SendRequestAsync(CancellationToken cancellationToken)
        {
            try
            {

                logger.LogInformation($"Sends request to distribution server at: {this.serverEndpoint}");
                byte[] bytes = BitConverter.GetBytes(Config.NumberOfPackages);
                await udpClientSender.SendAsync(bytes, this.serverEndpoint, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to send the request distribution server at: {this.serverEndpoint}");
            }
        }
    }
}
