using GTweens.Tweens;
using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Pipelines;
using System.Numerics;

namespace pixelflut.LiveStreamReactions;

public class Reaction
{
    public required PixelBuffer PixelBuffer { get; set; }
    public required TimeSpan StartTime { get; set; }
    public required TimeSpan Duration { get; set; }
    public GTween? Tween { get; set; }

    public required Image<Rgba32> Sprite { get; set; }
    public required System.Numerics.Vector2 StartPosition { get; set; }

    public required System.Numerics.Vector2 EndPosition { get; set; }

    public required System.Numerics.Vector2 CurrentPosition { get; set; }

    public void UpdateLocation(Reaction reaction, Vector2 newPosition)
    {
        reaction.CurrentPosition = newPosition;
        int pixelNumber = 0;
        for (int y = 0; y < reaction.Sprite.Height; y++)
        {
            for (int x = 0; x < reaction.Sprite.Width; x++)
            {
                var pixel = reaction.Sprite[x, y];
                if (pixel.A == 0) continue;

                reaction.PixelBuffer.ChangePixelPosition(
                    pixelNumber,
                    (int)newPosition.X + x,
                    (int)newPosition.Y + y);
                pixelNumber++;
            }
        }
    }
}
