using PixelFlut.Core;
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
        ILogger<DistributedWorker> logger)
    {
        this.screenProtocol = screenProtocol;
        Config = config;
        PixelFlutScreen = pixelFlutScreen;
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

    private async Task GetBuffersFromDistributionServerAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(serverEndpoint, cancellationToken);
                while (true)
                {
                    List<PixelBuffer> frame = await ReadResponseFromServerAsync(client, cancellationToken);
                    this.currentFrame = frame;
                }
            }
            catch (SocketException e)
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
            byte[] buffer = new byte[screenProtocol.PixelsPerBuffer];
            int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == DistributedServer.StopDelimitorBytes.Length)
            {
                break;
            }
            else if (bytesRead == screenProtocol.PixelsPerBuffer)
            {
                byte[] bufferCopy = new byte[screenProtocol.PixelsPerBuffer];
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