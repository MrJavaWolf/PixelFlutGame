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
    public const int SIO_UDP_CONNRESET = -1744830452;

    private IPEndPoint localEndpoint;
    private UdpClient udpClientSender;
    private UdpClient udpClientReciever;

    public DistributedServer(
        DistributedServerConfiguration config,
        PixelFlutScreen pixelFlutScreen,
        ILogger<DistributedServer> logger)
    {
        this.config = config;
        this.pixelFlutScreen = pixelFlutScreen;
        this.logger = logger;


        localEndpoint = new IPEndPoint(IPAddress.Any, config.UdpPort);
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
            while (true)
            {
                //logger.LogInformation($"Listing for workers on {localEndpoint}...");
                UdpReceiveResult result = await udpClientReciever.ReceiveAsync(cancellationToken);

                if (result.Buffer.Length < 4)
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
        try
        {
            //logger.LogInformation($"Sending {numberOfPackages} packages to {endpoint}");
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
                        await udpClientSender.SendAsync(buffer, endpoint, cancellationToken);
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to send frames to worker at endpoint '{endpoint}'");
        }
    }
}
