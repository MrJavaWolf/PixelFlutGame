namespace PixelFlut.Core;

record FrameBufferPosition(int buffer, int position);

public class FrameBuffer
{
    // Util
    private readonly IPixelFlutScreenProtocol screenProtocol;

    // Buffers used for sending the bytes
    private readonly List<byte[]> PreparedBuffers = new();

    /// <summary>
    /// To make the rendering look visually more pleaseing, we use a mapping from pixel number to actual buffer and index in buffer position .
    /// Instead of map a range of pixels next to each other into the same buffer, we split pixels next to each other into different buffers. 
    /// This list is to keep track of where each pixel is places in the buffers
    /// </summary>
    private List<FrameBufferPosition> mappings = new();

    /// <summary>
    /// The total number of pixels in this frame
    /// </summary>
    public int NumberOfPixels { get; }


    public FrameBuffer(int numberOfPixels, IPixelFlutScreenProtocol screenProtocol)
    {
        NumberOfPixels = numberOfPixels;
        this.screenProtocol = screenProtocol;

        // Create the buffers
        while (numberOfPixels > PreparedBuffers.Count * screenProtocol.PixelsPerBuffer)
        {
            PreparedBuffers.Add(screenProtocol.CreateBuffer());
        }

        // Create a list with all positions in a random order
        for (int i = 0; i < PreparedBuffers.Count; i++)
        {
            for (int j = 0; j < PreparedBuffers[i].Length; j++)
            {
                mappings.Insert(Random.Shared.Next(0, mappings.Count), new FrameBufferPosition(i, j));
            }
        }
    }


    public void SetPixel(int pixelNumber, int x, int y, byte r, byte g, byte b, byte a)
    {
        if (pixelNumber >= NumberOfPixels)
            throw new IndexOutOfRangeException(
                $"The Framebuffer have {NumberOfPixels} pixels, " +
                $"you tried to set pixel {pixelNumber}");

        FrameBufferPosition position = mappings[pixelNumber];
        byte[] buffer = PreparedBuffers[position.buffer];
        screenProtocol.WriteToBuffer(buffer, position.position, x, y, r, g, b, a);
    }
}
