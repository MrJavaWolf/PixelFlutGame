using System.Drawing;

namespace PixelFlut.Core;

record PixelBufferPosition(int buffer, int position);

public class PixelBuffer
{
    // The protocol we are using
    private readonly IPixelFlutScreenProtocol screenProtocol;

    // Screen config
    private readonly PixelFlutScreenConfiguration screenConfiguration;

    /// <summary>
    /// To make the rendering look visually more humanly pleaseing, we use a mapping from pixel number to actual buffer and index in buffer position .
    /// Instead of map a range of pixels next to each other into the same buffer, we split pixels next to each other into different buffers. 
    /// This list is to keep track of where each pixel is places in the buffers
    /// </summary>
    private PixelBufferPosition[] mappings;

    /// <summary>
    /// The total number of pixels in this buffer
    /// </summary>
    public int NumberOfPixels { get; }

    /// <summary>
    /// The buffers you can send to the pixel flut
    /// </summary>
    public IReadOnlyList<byte[]> Buffers { get => buffers; }

    // Buffers used for sending the bytes
    private readonly List<byte[]> buffers = new();

    public int PixelsPerBuffer { get => screenProtocol.PixelsPerBuffer; }

    public PixelBuffer(
        int numberOfPixels, 
        IPixelFlutScreenProtocol screenProtocol, 
        PixelFlutScreenConfiguration screenConfiguration)
    {
        if (numberOfPixels <= 0)
            throw new ArgumentException($"You cannot have {numberOfPixels} pixels in a framebuffer. " +
                $"The minimum number of allowed pixels is 1.");
        NumberOfPixels = numberOfPixels;
        this.screenProtocol = screenProtocol;
        this.screenConfiguration = screenConfiguration;

        // Create the buffers
        while (numberOfPixels > buffers.Count * screenProtocol.PixelsPerBuffer)
        {
            buffers.Add(screenProtocol.CreateBuffer());
        }

        // Create the mappings
        mappings = new PixelBufferPosition[buffers.Count * screenProtocol.PixelsPerBuffer];
        for (int i = 0; i < buffers.Count; i++)
        {
            for (int j = 0; j < screenProtocol.PixelsPerBuffer; j++)
            {
                mappings[i * screenProtocol.PixelsPerBuffer + j] = new PixelBufferPosition(i, j);
            }
        }

        // Shuffles the mappings
        FisherYatesShuffle(mappings);
    }

    /// <summary>
    /// Do an in-place shuffle
    /// https://stackoverflow.com/questions/273313/randomize-a-listt
    /// https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
    /// </summary>
    /// <param name="array">The array that will be shuffles</param>
    private void FisherYatesShuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }

    public void SetPixel(int pixelNumber, int X, int Y, byte R, byte G, byte B, byte A)
    {
        if (pixelNumber >= NumberOfPixels)
            throw new IndexOutOfRangeException(
                $"The {nameof(PixelBuffer)} have {NumberOfPixels} pixels, " +
                $"you tried to set pixel {pixelNumber}");

        PixelBufferPosition position = mappings[pixelNumber];
        byte[] buffer = buffers[position.buffer];
        screenProtocol.WriteToBuffer(
            buffer, 
            position.position, 
            X + screenConfiguration.OffsetX, 
            Y + screenConfiguration.OffsetY, 
            R, 
            G, 
            B, 
            A);
    }

    public void SetPixel(int pixelNumber, int X, int Y, Color c)
    {
        SetPixel(pixelNumber, X, Y, c.R, c.G, c.B, c.A);
    }
}
