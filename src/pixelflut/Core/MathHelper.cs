﻿using System.Drawing;
using System.Numerics;

namespace PixelFlut.Core;

public static class MathHelper
{
    public static float RemapRange(float from, float fromMin, float fromMax, float toMin, float toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
    }

    public static Vector2 Rotate(Vector2 v, double degrees)
    {
        return new Vector2(
            (float)(v.X * Math.Cos(degrees) - v.Y * Math.Sin(degrees)),
            (float)(v.X * Math.Sin(degrees) + v.Y * Math.Cos(degrees))
        );
    }

    public static Color Lerp(this Color startColor, Color endColor, float amount)
    {
        Vector4 vectorStart = new(startColor.R, startColor.G, startColor.B, startColor.A);
        Vector4 vectorEnd = new(endColor.R, endColor.G, endColor.B, endColor.A);
        Vector4 resultVector = Vector4.Lerp(vectorStart, vectorEnd, amount);
        Color resultColor = Color.FromArgb((int)resultVector.W, (int)resultVector.X, (int)resultVector.Y, (int)resultVector.Z);
        return resultColor;
    }

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, v, t, p);
        else if (hi == 1)
            return Color.FromArgb(255, q, v, p);
        else if (hi == 2)
            return Color.FromArgb(255, p, v, t);
        else if (hi == 3)
            return Color.FromArgb(255, p, q, v);
        else if (hi == 4)
            return Color.FromArgb(255, t, p, v);
        else
            return Color.FromArgb(255, v, p, q);
    }
}
