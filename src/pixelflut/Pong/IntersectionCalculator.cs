using System.Numerics;

namespace PixelFlut.Pong;

public static class IntersectionCalculator
{
    #region https://stackoverflow.com/a/402010
    /// <summary>
    /// Tests if a cirle and rectangle intersect eachother
    /// </summary>
    /// <param name="circleX"></param>
    /// <param name="circleY"></param>
    /// <param name="circleR"></param>
    /// <param name="rectX"></param>
    /// <param name="rectY"></param>
    /// <param name="rectWidth"></param>
    /// <param name="rectHeight"></param>
    /// <returns></returns>
    public static bool DoesCirlceAndRectangleIntersects(
        double circleX,
        double circleY,
        double circleR,
        double rectX,
        double rectY,
        double rectWidth,
        double rectHeight)
    {
        double circleDistanceX = Math.Abs(circleX - rectX);
        double circleDistanceY = Math.Abs(circleY - rectY);
        if (circleDistanceX > (rectWidth / 2 + circleR)) { return false; }
        if (circleDistanceY > (rectHeight / 2 + circleR)) { return false; }
        if (circleDistanceX <= (rectWidth / 2)) { return true; }
        if (circleDistanceY <= (rectHeight / 2)) { return true; }
        double cornerDistance_sq = Math.Pow(circleDistanceX - rectWidth / 2, 2) + Math.Pow(circleDistanceY - rectHeight / 2, 2);
        return (cornerDistance_sq <= Math.Pow(circleR, 2));
    }
    #endregion

    #region https://stackoverflow.com/a/67662332
    // Tuple<entryVector2, exitVector2, lineStatus>
    public static (Vector2 EntryPoint, Vector2 ExitPoint, Line lineStatus) GetIntersectionVector2(
        Vector2 a,
        Vector2 b,
        float rectX,
        float rectY,
        float rectWidth,
        float rectHeight)
    {
        if (IsWithinRectangle(a, rectX, rectY, rectWidth, rectHeight) && IsWithinRectangle(b, rectX, rectY, rectWidth, rectHeight))
        {
            // Can't set null to Vector2 that's why I am returning just empty object
            return (new Vector2(), new Vector2(), Line.InsideTheRectangle);
        }
        else if (!IsWithinRectangle(a, rectX, rectY, rectWidth, rectHeight) && !IsWithinRectangle(b, rectX, rectY, rectWidth, rectHeight))
        {
            if (!LineIntersectsRectangle(a, b, rectX, rectY, rectWidth, rectHeight))
            {
                // Can't set null to Vector2 that's why I am returning just empty object
                return (new Vector2(), new Vector2(), Line.NoIntersection);
            }

            Vector2 entryVector2 = new Vector2();
            Vector2 exitVector2 = new Vector2();

            bool entryVector2Found = false;

            // Top Line of Chart Area
            if (LineIntersectsLine(a, b, new Vector2(rectX, rectY), new Vector2(rectX + rectWidth, rectY)))
            {
                entryVector2 = GetVector2FromYValue(a, b, rectY);
                entryVector2Found = true;
            }
            // Right Line of Chart Area
            if (LineIntersectsLine(a, b, new Vector2(rectX + rectWidth, rectY), new Vector2(rectX + rectWidth, rectY + rectHeight)))
            {
                if (entryVector2Found)
                    exitVector2 = GetVector2FromXValue(a, b, rectX + rectWidth);
                else
                {
                    entryVector2 = GetVector2FromXValue(a, b, rectX + rectWidth);
                    entryVector2Found = true;
                }
            }
            // Bottom Line of Chart
            if (LineIntersectsLine(a, b, new Vector2(rectX, rectY + rectHeight), new Vector2(rectX + rectWidth, rectY + rectHeight)))
            {
                if (entryVector2Found)
                    exitVector2 = GetVector2FromYValue(a, b, rectY + rectHeight);
                else
                {
                    entryVector2 = GetVector2FromYValue(a, b, rectY + rectHeight);
                }
            }
            // Left Line of Chart
            if (LineIntersectsLine(a, b, new Vector2(rectX, rectY), new Vector2(rectX, rectY + rectHeight)))
            {
                exitVector2 = GetVector2FromXValue(a, b, rectX);
            }

            return (entryVector2, exitVector2, Line.EntryExit);
        }
        else
        {
            Vector2 entryVector2 = GetEntryIntersectionVector2(rectX, rectY, rectWidth, rectHeight, a, b);
            return (entryVector2, new Vector2(), Line.Entry);
        }
    }

    public enum Line
    {
        // Inside the Rectangle so No Intersection Vector2(Both Entry Vector2 and Exit Vector2 will be Null)
        InsideTheRectangle,

        // One Vector2 Inside the Rectangle another Vector2 Outside the Rectangle. So it has only Entry Vector2
        Entry,

        // Both Vector2 Outside the Rectangle but Intersecting. So It has both Entry and Exit Vector2
        EntryExit,

        // Both Vector2 Outside the Rectangle and not Intersecting. So doesn't has both Entry and Exit Vector2
        NoIntersection
    }

    private static Vector2 GetEntryIntersectionVector2(
        float rectX,
        float rectY,
        float rectWidth,
        float rectHeight,
        Vector2 a,
        Vector2 b)
    {
        // For top line of the rectangle
        if (LineIntersectsLine(new Vector2(rectX, rectY), new Vector2(rectX + rectWidth, rectY), a, b))
        {
            return GetVector2FromYValue(a, b, rectY);
        }
        // For right side line of the rectangle
        else if (LineIntersectsLine(new Vector2(rectX + rectWidth, rectY), new Vector2(rectX + rectWidth, rectY + rectHeight), a, b))
        {
            return GetVector2FromXValue(a, b, rectX + rectWidth);
        }
        // For bottom line of the rectangle
        else if (LineIntersectsLine(new Vector2(rectX, rectY + rectHeight), new Vector2(rectX + rectWidth, rectY + rectHeight), a, b))
        {
            return GetVector2FromYValue(a, b, rectY + rectHeight);
        }
        // For left side line of the rectangle
        else
        {
            return GetVector2FromXValue(a, b, rectX);
        }
    }

    public static bool LineIntersectsRectangle(
        Vector2 p1,
        Vector2 p2,
        float rectX,
        float rectY,
        float rectWidth,
        float rectHeight)
    {
        return LineIntersectsLine(p1, p2, new Vector2(rectX, rectY), new Vector2(rectX + rectWidth, rectY)) ||
               LineIntersectsLine(p1, p2, new Vector2(rectX + rectWidth, rectY), new Vector2(rectX + rectWidth, rectY + rectHeight)) ||
               LineIntersectsLine(p1, p2, new Vector2(rectX + rectWidth, rectY + rectHeight), new Vector2(rectX, rectY + rectHeight)) ||
               LineIntersectsLine(p1, p2, new Vector2(rectX, rectY + rectHeight), new Vector2(rectX, rectY));
    }

    private static bool LineIntersectsLine(Vector2 l1p1, Vector2 l1p2, Vector2 l2p1, Vector2 l2p2)
    {
        float q = (l1p1.Y - l2p1.Y) * (l2p2.X - l2p1.X) - (l1p1.X - l2p1.X) * (l2p2.Y - l2p1.Y);
        float d = (l1p2.X - l1p1.X) * (l2p2.Y - l2p1.Y) - (l1p2.Y - l1p1.Y) * (l2p2.X - l2p1.X);

        if (d == 0)
        {
            return false;
        }

        float r = q / d;

        q = (l1p1.Y - l2p1.Y) * (l1p2.X - l1p1.X) - (l1p1.X - l2p1.X) * (l1p2.Y - l1p1.Y);
        float s = q / d;

        if (r < 0 || r > 1 || s < 0 || s > 1)
        {
            return false;
        }

        return true;
    }

    // For Large values, processing with integer is not working properly
    // So I here I am dealing only with double for high accuracy
    private static Vector2 GetVector2FromYValue(Vector2 a, Vector2 b, double y)
    {
        double x1 = a.X, x2 = b.X, y1 = a.Y, y2 = b.Y;
        double x = (((y - y1) * (x2 - x1)) / (y2 - y1)) + x1;
        return new Vector2((int)x, (int)y);
    }

    // For Large values, processing with integer is not working properly
    // So here I am dealing only with double for high accuracy
    private static Vector2 GetVector2FromXValue(Vector2 a, Vector2 b, double x)
    {
        double x1 = a.X, x2 = b.X, y1 = a.Y, y2 = b.Y;
        double y = (((x - x1) * (y2 - y1)) / (x2 - x1)) + y1;
        return new Vector2((int)x, (int)y);
    }

    // rect.Contains(Vector2) is not working properly in some cases.
    // So here I created my own method
    private static bool IsWithinRectangle(
        Vector2 a,
        float rectX,
        float rectY,
        float rectWidth,
        float rectHeight)
    {
        return a.X >= rectX && a.X <= rectX + rectWidth && a.Y >= rectY && a.Y <= rectY + rectHeight;
    }


    #endregion
}