using PixelFlut.Core;
using System.Drawing;
using System.Numerics;

namespace PixelFlut.Effect;

public class SphereExplosionEffect : IEffect
{
    // Effect setup values
    private TimeSpan startTime;
    private Vector2 center;
    private float startRadius;
    private float endRadius;
    private TimeSpan lifeTime;
    private Color startColor;
    private Color endColor;

    // Effect runtime values
    public bool IsAlive { get; private set; }
    public float currentRaidus;
    public Color currentColor;

    // Core
    public PixelBuffer PixelBuffer { get; }
    private int amountOfPixelsInEffect;

    public SphereExplosionEffect(
        int amountOfPixelsInEffect,
        PixelBufferFactory bufferFactory)
    {
        this.amountOfPixelsInEffect = amountOfPixelsInEffect;
        PixelBuffer = bufferFactory.Create(this.amountOfPixelsInEffect);
    }

    public void Start(
        GameTime time,
        Vector2 center,
        int startRadius,
        int endRadius,
        TimeSpan lifeTime,
        Color startColor,
        Color endColor)
    {
        this.startTime = time.TotalTime;
        this.center = center;
        this.startRadius = startRadius;
        this.endRadius = endRadius;
        this.lifeTime = lifeTime;
        this.startColor = startColor;
        this.endColor = endColor;
        IsAlive = true;
    }

    public void Loop(GameTime gameTime)
    {
        if (gameTime.TotalTime > startTime + lifeTime)
        {
            IsAlive = false;
            currentColor = endColor;
            currentRaidus = endRadius;
            return;
        }
        float lifeTimeRatio = (float)((gameTime.TotalTime.TotalSeconds - startTime.TotalSeconds) / lifeTime.TotalSeconds);
        this.currentRaidus = MathHelper.RemapRange(lifeTimeRatio, 0, 1, startRadius, endRadius);
        currentColor = startColor.Lerp(endColor, (float)lifeTimeRatio);
    }

    public void Renderer()
    {
        // Draws the circle
        double stepSize = Math.PI * 2 / amountOfPixelsInEffect;
        for (int i = 0; i < amountOfPixelsInEffect; i++)
        {
            double currentAngle = stepSize * i;
            int x = (int)(currentRaidus * Math.Cos(currentAngle) + center.X);
            int y = (int)(currentRaidus * Math.Sin(currentAngle) + center.Y);
            PixelBuffer.SetPixel(i, x, y, currentColor);
        }
    }
}
