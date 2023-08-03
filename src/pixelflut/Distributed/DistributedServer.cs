using PixelFlut.Core;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PixelFlut.Distributed;

public class DistributedServerConfiguration
{
    public bool Enable { get; set; }
    public int UdpPort { get; set; }
}

public class DistributedServer
{
    private readonly DistributedServerConfiguration config;
    private readonly PixelFlutScreen pixelFlutScreen;
    private readonly ILogger<DistributedServer> logger;
    private const int listenPort = 11000;

    public DistributedServer(
        DistributedServerConfiguration config,
        PixelFlutScreen pixelFlutScreen,
        ILogger<DistributedServer> logger)
    {
        this.config = config;
        this.pixelFlutScreen = pixelFlutScreen;
        this.logger = logger;
    }

    public void Start(CancellationToken cancellation)
    {
        if (!config.Enable) return;

        Task t = Task.Run(async () => await StartListenerAsync(cancellation));
    }

    private async Task StartListenerAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(DistributedServer)} configuration: {{@config}}", config);
        try
        {
            using UdpClient listener = new UdpClient(config.UdpPort);
            while (true)
            {
                logger.LogInformation("Waiting for broadcast...");
                UdpReceiveResult result = await listener.ReceiveAsync(cancellationToken);

                logger.LogInformation($"Received broadcast from {result.RemoteEndPoint} :");
                logger.LogInformation($" {Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length)}");
                if (result.Buffer.Length != 4)
                    continue;

                int numberOfPackages = BitConverter.ToInt32(result.Buffer, 0);

                _ = Task.Run(async () => await SendFramesAsync(numberOfPackages, result.RemoteEndPoint, cancellationToken));
            }
        }
        catch (SocketException e)
        {
            logger.LogError(e, "Udp client failed");
        }
    }

    private async Task SendFramesAsync(int numberOfPackages, IPEndPoint endpoint, CancellationToken cancellationToken)
    {
        using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        for (int i = 0; i < numberOfPackages; i++)
        {
            var currentFrame = pixelFlutScreen.CurrentFrame;
            if (currentFrame.Count > 0)
            {
                int frameIndex = Random.Shared.Next(0, currentFrame.Count);
                var pixelBuffers = currentFrame[frameIndex].Buffers;
                if (pixelBuffers.Count > 0)
                {
                    int pixelBufferIndex = Random.Shared.Next(0, pixelBuffers.Count);
                    byte[] buffer = pixelBuffers[pixelBufferIndex];
                    await sock.SendToAsync(buffer, endpoint, cancellationToken);
                }
            }
        }
    }
}
