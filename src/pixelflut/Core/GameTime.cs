namespace PixelFlut.Core;

public class GameTime
{
    /// <summary>
    /// How long time the game have been running
    /// </summary>
    public TimeSpan TotalTime { get; set; }

    /// <summary>
    /// How long the last frame took
    /// </summary>
    public TimeSpan DeltaTime { get; set; }
}
