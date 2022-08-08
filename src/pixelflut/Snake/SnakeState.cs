using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelFlut.Snake;

public class SnakeState
{
    public enum Directions
    {
        Left,
        Right,
        Up,
        Down
    };
    public List<(int X, int Y)> Snake { get; set; } = new();
    public Directions Direction { get; set; }
    public TimeSpan TimeBetweenSteps { get; set; }
    public (int X, int Y) Food { get; set; }
    public (int Width, int Height) AreaSize { get; set; }
    public TimeSpan LastMoveTime { get; set; } = TimeSpan.Zero;

}
