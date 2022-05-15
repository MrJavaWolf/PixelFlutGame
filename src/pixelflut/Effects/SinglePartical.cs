using PixelFlut.Core;
using System.Drawing;
using System.Numerics;

namespace PixelFlut.Effect;

public class SinglePartical
{
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    public float Speed { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan LifeTime { get; set; }
    public TimeSpan DeathTime { get => StartTime + LifeTime; }
    public bool IsAlive { get; set; } = true;
    public Color StartColor { get; set; }
    public Color EndColor { get; set; }
    public Color CurrentColor { get; set; }

    public SinglePartical() { }
    public SinglePartical(SinglePartical partical)
    {
        this.StartTime = partical.StartTime;
        this.LifeTime = partical.LifeTime;
        this.Speed = partical.Speed;
        this.IsAlive = partical.IsAlive;
        this.Position = partical.Position;
        this.Direction = partical.Direction;
        this.StartColor = partical.StartColor;
        this.EndColor = partical.EndColor;
        this.CurrentColor = partical.CurrentColor;
    }

    public void Start(
        Vector2 position,
        Vector2 direction,
        TimeSpan startTime)
    {
        this.Position = position;
        this.Direction = direction;
        this.StartTime = startTime;
        this.IsAlive = true;
    }

    public void Loop(GameTime gameTime)
    {
        if (gameTime.TotalTime > DeathTime)
        {
            IsAlive = false;
            CurrentColor = EndColor;
            return;
        }
        double lifeTimeRatio = (gameTime.TotalTime.TotalSeconds - StartTime.TotalSeconds) / LifeTime.TotalSeconds;
        Position += Direction * Speed * (float)gameTime.DeltaTime.TotalSeconds;
        CurrentColor = StartColor.Lerp(EndColor, (float)lifeTimeRatio);
    }
}
