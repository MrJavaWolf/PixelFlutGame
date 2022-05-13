using PixelFlut.Core;
using System.Drawing;
using System.Numerics;

namespace PixelFlut.Pong
{
    public class PongBallBounceParticalEffect
    {
        public record PongBallBounceEffectType(
            Color startColor,
            Color endColor,
            TimeSpan lifeTime,
            float speed,
            int spread,
            int numberOfParticals);

        public static readonly PongBallBounceEffectType PlayerBounce = new(
            Color.White,
            Color.FromArgb(0, 0, 0, 0),
            TimeSpan.FromSeconds(0.3),
            200,
            360,
            15);

        public static readonly PongBallBounceEffectType WallBounce = new(
           Color.White,
           Color.FromArgb(0, 0, 0, 0),
           TimeSpan.FromSeconds(0.3),
           200,
           360,
           15);

        public static readonly PongBallBounceEffectType Goal = new(
           Color.White,
           Color.FromArgb(0, 0, 0, 0),
           TimeSpan.FromSeconds(0.5),
           700,
           360,
           50);

        private readonly PongBallBounceEffectType effectType;
        private List<SinglePartical> particals = new();

        public bool IsAlive { get => particals.Any(p => p.IsAlive); }
        public readonly PixelBuffer PixelBuffer;

        public PongBallBounceParticalEffect(
            PongBallBounceEffectType effectType,
            IPixelFlutScreenProtocol screenProtocol)
        {
            PixelBuffer = new PixelBuffer(effectType.numberOfParticals, screenProtocol);
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

            this.effectType = effectType;
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
                    Random.Shared.Next(0, effectType.spread) *
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

        public PixelBuffer Renderer()
        {
            for (int i = 0; i < particals.Count; i++)
            {
                SinglePartical partical = particals[i];
                PixelBuffer.SetPixel(i,
                    (int)partical.Position.X,
                    (int)partical.Position.Y,
                    partical.CurrentColor);
            }
            return PixelBuffer;
        }
    }
}
