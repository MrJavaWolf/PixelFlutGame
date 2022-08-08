namespace PixelFlut.Snake;

public class SnakeConfiguration
{
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public int TileBorderSize { get; set; }
    public int SnakeStartSize { get; set; }
    public TimeSpan StartTimeBetweenSteps { get; set; }
    public float TimeBetweenStepsDecreasePerFood { get; set; }
}
