using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PixelFlut.Core;

public class PixelFlutScreenTcpSocket : IPixelFlutScreenSocket
{
    private readonly PixelFlutScreenConfiguration configuration;
    private readonly ILogger logger;

    // Connection
    private TcpClient tcpClient;
    private IPEndPoint endPoint;

    private int currentRenderFrameBuffer = 0;
    private int currentRenderByteBuffer = 0;
    private bool isConnected = false;


    public PixelFlutScreenTcpSocket(PixelFlutScreenConfiguration configuration, ILogger logger)
    {
        this.configuration = configuration;
        this.logger = logger;

        // Setup connnection
        tcpClient = new TcpClient();
        IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
        int port = ReadPort(configuration);
        endPoint = new IPEndPoint(serverAddr, port);
        logger.LogInformation($"PixelFlutScreen using endpoint: {{@endPoint}}", endPoint);
    }

    private static int ReadPort(PixelFlutScreenConfiguration configuration)
    {
        string portErrorMessage = $"Not a valid port '{configuration.Port}', please check your configuration. A valid port is a port between 1 and 65000 (Example: 5000), or a port range (Example: 5000-5999)";
        int port = 0;
        if (string.IsNullOrWhiteSpace(configuration.Port))
            throw new ArgumentException(portErrorMessage);
        if (configuration.Port.Contains("-"))
        {
            string[] portRange = configuration.Port.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (portRange.Length != 2)
                throw new ArgumentException(portErrorMessage);

            if (!int.TryParse(portRange[0], out int portMin) ||
                !int.TryParse(portRange[1], out int portMax))
                throw new ArgumentException(portErrorMessage);
            port = Random.Shared.Next(portMin, portMax + 1);
        }
        else
        {
            if (!int.TryParse(configuration.Port, out port))
                throw new ArgumentException(portErrorMessage);
        }

        return port;
    }

    public void Render(List<PixelBuffer> frame, PixelFlutScreenStats stats)
    {
        if (frame.Count == 0) return;
        int numberOfBuffers = 0;
        foreach (var buffer in frame)
        {
            numberOfBuffers += buffer.Buffers.Count;
            if (numberOfBuffers > 0) break;
        }
        if (numberOfBuffers == 0) return;

        if (!isConnected)
        {
            try
            {
                tcpClient.Dispose();
                tcpClient = new TcpClient();
                logger.LogInformation($"Creates TCP connection to {endPoint}...");
                tcpClient.Connect(endPoint);
                logger.LogInformation($"Successfully connected to TCP {endPoint}");
                isConnected = true;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to connect to TCP {endPoint}");
                isConnected = false;
                throw;
            }
        }

        // Pick a buffer to render
        (int pixels, byte[] sendBuffer) = SelectNextBuffer(frame);

        try
        {
            // Send 
            int bytesSent = tcpClient.Client.Send(sendBuffer);

            // Update stats
            stats.BytesSent += bytesSent;
            stats.PixelsSent += pixels;
            stats.BuffersSent++;
            stats.TotalBuffersSent++;

            //string result = System.Text.UTF8Encoding.UTF8.GetString(sendBuffer, 0, sendBuffer.Length);
            //logger.LogInformation($"Bytes send: {result}");
        }
        catch (Exception ex)
        {
            string result = System.Text.UTF8Encoding.UTF8.GetString(sendBuffer, 0, sendBuffer.Length);
            logger.LogError(ex, $"TCP failed to send: '{result}'");
            isConnected = false;
            throw;
        }


        // Wait if requested, usefull on slower single core CPU's
        if (configuration.SleepTimeBetweenSends != -1)
        {
            Thread.Sleep(configuration.SleepTimeBetweenSends);
        }
    }

    private (int pixels, byte[] sendBuffer) SelectNextBuffers(List<PixelBuffer> frame)
    {

        int pixelsToSend = 0;
        int bytesToSend = 0;
        List<byte[]> bytesList = new List<byte[]>();
        while (bytesToSend < 1000)
        {

            (int pixels, byte[] sendBuffer) = SelectNextBuffer(frame);
            pixelsToSend += pixels;
            bytesToSend += sendBuffer.Length;
            bytesList.Add(sendBuffer);
        }

        byte[] bytes = new byte[bytesToSend];
        int index = 0;
        foreach (byte[] b in bytesList)
        {
            Array.Copy(b, 0, bytes, index, b.Length);
            index += b.Length;
        }

        return (pixelsToSend, bytes);
    }

    private (int pixels, byte[] sendBuffer) SelectNextBuffer(List<PixelBuffer> frame)
    {
        // Ensures we reset if the frame changes
        if (frame.Count <= currentRenderFrameBuffer)
        {
            currentRenderFrameBuffer = 0;
            currentRenderByteBuffer = 0;
        }

        // Gets the buffer
        PixelBuffer buffer = frame[currentRenderFrameBuffer];

        // Ensures we reset if the frame changes
        if (buffer.Buffers.Count <= currentRenderByteBuffer)
        {
            currentRenderByteBuffer = 0;
        }

        // Sends the buffer
        byte[] sendBuffer = buffer.Buffers[currentRenderByteBuffer];
        int pixelsPerBuffer = buffer.PixelsPerBuffer;
        IncrementBufferIndex(frame, buffer);

        return (pixelsPerBuffer, sendBuffer);
    }

    private void IncrementBufferIndex(List<PixelBuffer> frame, PixelBuffer buffer)
    {
        // Increment to select the next buffer
        currentRenderByteBuffer++;
        if (currentRenderByteBuffer >= buffer.Buffers.Count)
        {
            currentRenderByteBuffer = 0;
            currentRenderFrameBuffer++;
            if (currentRenderFrameBuffer >= frame.Count)
            {
                currentRenderFrameBuffer = 0;
            }
        }
    }
}
