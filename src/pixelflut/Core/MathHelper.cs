using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PixelFlut.Core
{
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
    }
}
