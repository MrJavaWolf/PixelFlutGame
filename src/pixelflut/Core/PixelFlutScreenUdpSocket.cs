﻿using System.Net;
using System.Net.Sockets;

namespace PixelFlut.Core;

public class PixelFlutScreenUdpSocket : IPixelFlutScreenSocket
{
    private readonly PixelFlutScreenConfiguration configuration;

    // Connection
    private Socket socket;
    private IPEndPoint endPoint;

    private int currentRenderFrameBuffer = 0;
    private int currentRenderByteBuffer = 0;


    public PixelFlutScreenUdpSocket(PixelFlutScreenConfiguration configuration, ILogger logger)
    {
        this.configuration = configuration;

        // Setup connnection
        socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
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