using PixelFlut.Core;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PixelFlut.Distributed;

public class DistributedWorkerConfiguration
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; }
}

public class DistributedWorker : IGame
{
    private readonly IPixelFlutScreenProtocol screenProtocol;
    private readonly PixelBufferFactory bufferFactory;
    private readonly ILogger<DistributedWorker> logger;
    public DistributedWorkerConfiguration Config { get; }
    public PixelFlutScreen PixelFlutScreen { get; }
    private IPEndPoint serverEndpoint;

    private List<PixelBuffer> currentFrame = new List<PixelBuffer>();
    private Task? readFromSocketAndGetFrameTask;

    public DistributedWorker(
        IPixelFlutScreenProtocol screenProtocol,
        DistributedWorkerConfiguration config,
        PixelFlutScreen pixelFlutScreen,
        PixelBufferFactory bufferFactory,
        ILogger<DistributedWorker> logger)
    {
        this.screenProtocol = screenProtocol;
        Config = config;
        PixelFlutScreen = pixelFlutScreen;
        this.bufferFactory = bufferFactory;
        this.logger = logger;
        logger.LogInformation($"{nameof(DistributedWorker)} configuration: {{@config}}", Config);
        serverEndpoint = new IPEndPoint(IPAddress.Parse(Config.Ip), Config.Port);
    }


    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        if (readFromSocketAndGetFrameTask == null)
        {
            readFromSocketAndGetFrameTask = Task.Run(async () => await GetBuffersFromDistributionServerAsync());
        }
        return this.currentFrame;
    }

    private async Task GetBuffersFromDistributionServerAsync()
    {
        while (true)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(5000);
            try
            {
                using TcpClient client = new TcpClient();
                logger.LogInformation($"Connecting to server: {serverEndpoint}");
                await client.ConnectAsync(serverEndpoint, cancellationTokenSource.Token);
                logger.LogInformation($"Successfully connected to server: {serverEndpoint}");
                while (true)
                {
                    using CancellationTokenSource responseCancellationTokenSource = new CancellationTokenSource(5000);
                    List<PixelBuffer> frame = await ReadResponseFromServerAsync(client, responseCancellationTokenSource.Token);
                    this.currentFrame = frame;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to connect with Server");
                await Task.Delay(5000);
            }
        }
    }

    private async Task<List<PixelBuffer>> ReadResponseFromServerAsync(TcpClient client, CancellationToken cancellationToken)
    {
        List<byte[]> bytes = new List<byte[]>();
        while (true)
        {
            byte[] buffer = new byte[screenProtocol.BufferSize];
            int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == DistributedServer.StopDelimitorBytes.Length)
            {
                break;
            }
            else if (bytesRead == screenProtocol.BufferSize)
            {
                byte[] bufferCopy = new byte[screenProtocol.BufferSize];
                Array.Copy(buffer, bufferCopy, bufferCopy.Length);
                bytes.Add(bufferCopy);
            }
        }
        return new List<PixelBuffer>
        {
            new PixelBuffer(screenProtocol.PixelsPerBuffer * bytes.Count, screenProtocol, bytes)
        };
    }

}