namespace PixelFlut.Core;

record PixelBufferPosition(int buffer, int position);

public class PixelBuffer
{
    // The protocol we are using
    private readonly IPixelFlutScreenProtocol screenProtocol;

    // Buffers used for sending the bytes
    private readonly List<byte[]> buffers = new();

    /// <summary>
    /// To make the rendering look visually more pleaseing, we use a mapping from pixel number to actual buffer and index in buffer position .
    /// Instead of map a range of pixels next to each other into the same buffer, we split pixels next to each other into different buffers. 
    /// This list is to keep track of where each pixel is places in the buffers
    /// </summary>
    private List<PixelBufferPosition> mappings = new();

    /// <summary>
    /// The total number of pixels in this buffer
    /// </summary>
    public int NumberOfPixels { get; }

    /// <summary>
    /// The buffers you can send to the pixel flut
    /// </summary>
    public IReadOnlyList<byte[]> Buffers { get => buffers; }

    public PixelBuffer(int numberOfPixels, IPixelFlutScreenProtocol screenProtocol)
    {
        if (numberOfPixels <= 0)
            throw new ArgumentException($"You cannot have {numberOfPixels} pixels in a framebuffer. " +
                $"The minimum number of allowed pixels is 1.");
        NumberOfPixels = numberOfPixels;
        this.screenProtocol = screenProtocol;

        // Create the buffers
        while (numberOfPixels > buffers.Count * screenProtocol.PixelsPerBuffer)
        {
            buffers.Add(screenProtocol.CreateBuffer());
        }

        // Create the mappings
        List<PixelBufferPosition> temp = new List<PixelBufferPosition>();
        for (int i = 0; i < buffers.Count; i++)
        {
            for (int j = 0; j < screenProtocol.PixelsPerBuffer; j++)
            {
                temp.Add(new PixelBufferPosition(i, j));
            }
        }

        // Shuffles the mappings
        mappings = temp.OrderBy(p => Random.Shared.Next(0, temp.Count)).ToList();
    }

    public void SetPixel(int pixelNumber, int X, int Y, byte R, byte G, byte B, byte A)
    {
        if (pixelNumber >= NumberOfPixels)
            throw new IndexOutOfRangeException(
                $"The {nameof(PixelBuffer)} have {NumberOfPixels} pixels, " +
                $"you tried to set pixel {pixelNumber}");

        PixelBufferPosition position = mappings[pixelNumber];
        byte[] buffer = buffers[position.buffer];
        screenProtocol.WriteToBuffer(buffer, position.position, X, Y, R, G, B, A);
    }
}
