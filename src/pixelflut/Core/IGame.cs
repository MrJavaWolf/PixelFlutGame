namespace PixelFlut.Core
{
    public interface IGame
    {
        /// <summary>
        /// Is called onces a frame
        /// </summary>
        /// <param name="time">How long time have eclipsed totally and since last frame</param>
        /// <param name="gamePads">A list of connected gamepads</param>
        /// <returns>A list of pixel buffers (/ A Frame) to be rendered</returns>
        List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads);
    }
}
