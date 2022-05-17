using PixelFlut.Core;
using System.Drawing;
using System.Numerics;

namespace PixelFlut.Effect;

public class IndividualParticalExplosionEffect : IEffect
{
    public record EffectData(
        Color startColor,
        Color endColor,
        TimeSpan lifeTime,
        float speed,
        int spread,
        int numberOfParticals);


    public static readonly EffectData Goal = new(
       Color.White,
       Color.FromArgb(0, 0, 0, 0),
       TimeSpan.FromSeconds(0.5),
       700,
       360,
       50);

    private readonly EffectData effectData;
    private List<SinglePartical> particals = new();

    public bool IsAlive { get => particals.Any(p => p.IsAlive); }
    public PixelBuffer PixelBuffer { get; private set; }

    public IndividualParticalExplosionEffect(
        EffectData effectType,
        PixelBufferFactory bufferFactory)
    {
        PixelBuffer = bufferFactory.Create(effectType.numberOfParticals);
        for (int i = 0; i < effectType.numberOfParticals; i++)
        {
            particals.Add(new SinglePartical()
            {
                Position = Vector2.Zero,
                Direction = Vector2.Zero,
                StartTime = TimeSpan.Zero,
                LifeTime = effectType.lifeTime,
                Speed = effectType.speed,
                StartColor = effectType.startColor,
                CurrentColor = effectType.startColor,
                EndColor = effectType.endColor,
                IsAlive = false,
            });
        }

        this.effectData = effectType;
    }

    public void Start(
        Vector2 position,
        Vector2 direction,
        TimeSpan startTime)
    {
        for (int i = 0; i < particals.Count; i++)
        {
            Vector2 randomizedDirection = MathHelper.Rotate(
                direction,
                Random.Shared.Next(0, effectData.spread) *
                (Random.Shared.NextDouble() > 0.5 ? 1 : -1));

            particals[i].Start(position, randomizedDirection, startTime);
        }
    }

    public void Loop(GameTime gameTime)
    {
        for (int i = 0; i < particals.Count; i++)
        {
            particals[i].Loop(gameTime);
        }
    }

    public void Renderer()
    {
        for (int i = 0; i < particals.Count; i++)
        {
            SinglePartical partical = particals[i];
            PixelBuffer.SetPixel(i,
                (int)partical.Position.X,
                (int)partical.Position.Y,
                partical.CurrentColor);
        }
    }
}
