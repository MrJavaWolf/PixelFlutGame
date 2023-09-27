using System.Numerics;

namespace PixelFlut.Core;

public static class Vector2Extensions
{
    public static float SignedAngle(this Vector2 a, Vector2 b)
    {
        float angle = 
            MathF.Atan2(
                Cross(a.X, a.Y, b.X, b.Y), 
                Dot(a.X, a.Y, b.X, b.Y)) 
            * 180f / MathF.PI;
        return angle;
    }

    public static float Dot(float ax, float ay, float bx, float by)
    {
        return ax * bx + ay * by;
    }
    public static float Cross(float ax, float ay, float bx, float by)
    {
        return ax * by - ay * bx;
    }
}
