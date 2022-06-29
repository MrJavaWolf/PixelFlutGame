using System.Net;
using System.Net.Sockets;

namespace PixelFlut.Core;

public class PixelFlutScreenSender
{
    private readonly PixelFlutScreenConfiguration configuration;

    // Connection
    private Socket socket;
    private IPEndPoint endPoint;

    private int currentRenderFrameBuffer = 0;
    private int currentRenderByteBuffer = 0;


    public PixelFlutScreenSender(PixelFlutScreenConfiguration configuration)
    {
        this.configuration = configuration;

        // Setup connnection
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress serverAddr = IPAddress.Parse(configuration.Ip);
        endPoint = new IPEndPoint(serverAddr, configuration.Port);
    }

    public void Render(List<PixelBuffer> frame, PixelFlutScreenStats stats)
    {
        if (frame.Count == 0) return;

        // Pick a buffer to render
        (int pixels, byte[] sendBuffer) = SelectNextBuffer(frame);

        // Send 
        int bytesSent = socket.SendTo(sendBuffer, endPoint);

        // Update stats
        stats.BytesSent += bytesSent;
        stats.PixelsSent += pixels;
        stats.BuffersSent++;
        stats.TotalBuffersSent++;

        // Wait if requested, usefull on slower single core CPU's
        if (configuration.SleepTimeBetweenSends != -1)
        {
            Thread.Sleep(configuration.SleepTimeBetweenSends);
        }
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