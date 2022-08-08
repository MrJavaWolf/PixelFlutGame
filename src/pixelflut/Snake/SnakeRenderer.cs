using PixelFlut.Core;
using System.Drawing;


namespace PixelFlut.Snake;


public class SnakeRendererBuffers
{
    public PixelBuffer[,] SnakeBuffer { get; set; } = null!;
    public PixelBuffer[,] BlankBuffers { get; set; } = null!;
    public PixelBuffer[,] FoodBuffers { get; set; } = null!;
}

public static class SnakeRenderer
{

    static readonly Color SnakeColor = Color.White;
    static readonly Color BlankColor = Color.Black;
    static readonly Color BorderColor = Color.Black;
    static readonly Color FoodColor = Color.Yellow;

    public static SnakeRendererBuffers PrepareBuffers(
        PixelBufferFactory bufferFactory,
        SnakeConfiguration snakeConfiguration,
        SnakeState snakeState)
    {
        PixelBuffer[,] snakeBuffers = CreateBuffers(bufferFactory, snakeConfiguration, snakeState, SnakeColor);
        PixelBuffer[,] blankBuffers = CreateBuffers(bufferFactory, snakeConfiguration, snakeState, BlankColor);
        PixelBuffer[,] foodBuffers = CreateBuffers(bufferFactory, snakeConfiguration, snakeState, FoodColor);
        return new SnakeRendererBuffers()
        {
            BlankBuffers = blankBuffers,
            FoodBuffers = foodBuffers,
            SnakeBuffer = snakeBuffers
        };
    }

    private static PixelBuffer[,] CreateBuffers(PixelBufferFactory bufferFactory,
        SnakeConfiguration snakeConfiguration,
        SnakeState snakeState,
        Color color)
    {
        PixelBuffer[,] buffers = new PixelBuffer[snakeState.AreaSize.Height + 1, snakeState.AreaSize.Width + 1];
        for (int y = 0; y < snakeState.AreaSize.Height + 1; y++)
        {
            for (int x = 0; x < snakeState.AreaSize.Width + 1; x++)
            {
                buffers[y, x] = CreateBuffer(
                    bufferFactory,
                    snakeConfiguration,
                    x * snakeConfiguration.TileWidth,
                    y * snakeConfiguration.TileHeight,
                    color);
            }
        }
        return buffers;
    }

    private static PixelBuffer CreateBuffer(
        PixelBufferFactory bufferFactory,
        SnakeConfiguration snakeConfiguration,
        int xOffset,
        int yOffset,
        Color color)
    {
        PixelBuffer buffer = bufferFactory.Create(snakeConfiguration.TileWidth * snakeConfiguration.TileHeight);
        int pixelNumber = 0;
        for (int y = 0; y < snakeConfiguration.TileHeight; y++)
        {
            for (int x = 0; x < snakeConfiguration.TileWidth; x++)
            {
                Color pixelColor = color;
                if (x <= snakeConfiguration.TileBorderSize ||
                    y <= snakeConfiguration.TileBorderSize ||
                    x >= snakeConfiguration.TileWidth - snakeConfiguration.TileBorderSize ||
                    y >= snakeConfiguration.TileHeight - snakeConfiguration.TileBorderSize)
                    pixelColor = BorderColor;

                buffer.SetPixel(pixelNumber, x + xOffset, y + yOffset, pixelColor);
                pixelNumber++;
            }
        }

        return buffer;
    }

    public static List<PixelBuffer> Renderer(
        SnakeRendererBuffers buffers,
        SnakeState snakeState)
    {
        List<PixelBuffer> buffersToRender = new List<PixelBuffer>();
        foreach (var snakePart in snakeState.Snake)
        {
            buffersToRender.Add(buffers.SnakeBuffer[snakePart.Y, snakePart.X]);
        }
        buffersToRender.Add(buffers.FoodBuffers[snakeState.Food.Y, snakeState.Food.X]);
        return buffersToRender;
    }
}
