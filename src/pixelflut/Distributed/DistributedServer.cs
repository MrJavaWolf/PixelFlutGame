using PixelFlut.Core;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PixelFlut.Distributed;

public class DistributedServerConfiguration
{
    public bool Enable { get; set; }
    public int Port { get; set; }

    public int NumberOfBuffersPerFrame { get; set; } = -1;
}


public class DistributedServer
{
    public const int stopDeliminator = 1337;
    public static byte[] StopDelimitorBytes { get; set; } = BitConverter.GetBytes(stopDeliminator);
    private readonly DistributedServerConfiguration config;
    private readonly PixelFlutScreen pixelFlutScreen;
    private readonly ILogger<DistributedServer> logger;
    public const int SIO_UDP_CONNRESET = -1744830452;

    private TcpListener server;
    private List<TcpClient> clients = new List<TcpClient>();

    public DistributedServer(
        DistributedServerConfiguration config,
        PixelFlutScreen pixelFlutScreen,
        ILogger<DistributedServer> logger)
    {
        this.config = config;
        this.pixelFlutScreen = pixelFlutScreen;
        this.logger = logger;

        server = new TcpListener(IPAddress.Any, config.Port);
    }

    public void Start(CancellationToken cancellation)
    {
        if (!config.Enable) return;

        Task t = Task.Run(async () => await StartListenerAsync(cancellation));
    }

    private async Task StartListenerAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(DistributedServer)} configuration: {{@config}}", config);

        // Start listening for client requests.
        server.Start();

        try
        {
            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync(cancellationToken);
                logger.LogInformation($"New client conncted from: {client.Client.RemoteEndPoint}");
                clients.Add(client);
            }
        }
        catch (SocketException e)
        {
            logger.LogError(e, "Udp client failed");
        }
    }


    public void SyncFrames()
    {
        if (!config.Enable) return;
        ConcurrentBag<TcpClient> deadClients = new ConcurrentBag<TcpClient>();
        Parallel.ForEach(clients, (client) =>
        {
            bool failed = false;
            if (!client.Connected) return;
            if (config.NumberOfBuffersPerFrame != -1)
            {
                for (int i = 0; i < config.NumberOfBuffersPerFrame; i++)
                {
                    if (failed) break;
                    var currentFrame = pixelFlutScreen.CurrentFrame;
                    if (currentFrame.Count > 0)
                    {
                        int frameIndex = Random.Shared.Next(0, currentFrame.Count);
                        var pixelBuffers = currentFrame[frameIndex].Buffers;
                        if (pixelBuffers.Count > 0)
                        {
                            int pixelBufferIndex = Random.Shared.Next(0, pixelBuffers.Count);
                            byte[] buffer = pixelBuffers[pixelBufferIndex];
                            try
                            {
                                client.GetStream().Write(buffer, 0, buffer.Length);
                            }
                            catch (Exception e)
                            {
                                logger.LogError(e, $"Failed to sync frames to client: {client.Client.RemoteEndPoint}");
                                deadClients.Add(client);
                                failed = true;
                            }
                        }
                    }
                }
            }
            else
            {
                var currentFrame = pixelFlutScreen.CurrentFrame;
                if (currentFrame.Count > 0)
                {
                    foreach (var frame in currentFrame)
                    {
                        if (failed) break;
                        foreach (var buffer in frame.Buffers)
                        {
                            if (failed) break;
                            try
                            {
                                client.GetStream().Write(buffer, 0, buffer.Length);
                            }
                            catch (Exception e)
                            {
                                logger.LogError(e, $"Failed to sync frames to client: {client.Client.RemoteEndPoint}");
                                deadClients.Add(client);
                                failed = true;
                            }
                        }
                    }
                }
            }

            try
            {
                client.GetStream().Write(StopDelimitorBytes, 0, StopDelimitorBytes.Length);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to sync frames to client: {client.Client.RemoteEndPoint}");
                deadClients.Add(client);
                failed = true;
            }

        });

        foreach (var deadClient in deadClients)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Client.RemoteEndPoint == deadClient.Client.RemoteEndPoint)
                {
                    this.clients.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
