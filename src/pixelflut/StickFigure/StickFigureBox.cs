using Humper;
using System.Drawing;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureBox
{
    public Vector2 Position { get; set; }

    public Vector2 Size { get; set; }

    public RectangleF Bounds => new RectangleF(Position.X, Position.Y, Size.X, Size.Y);

    public IBox Box { get; set; }

    public void Start()
    {
        Position = transform.position;
        Size = transform.localScale;
    }
}
