using System.Numerics;

namespace PixelFlut.Core;

public interface IGamePadDevice
{
    /// <summary>
    /// The horizontal amount, range 0 - 1 where
    /// 0   = All left
    /// 0.5 = Middel
    /// 1   = All Right
    /// </summary>
    double X { get; }

    /// <summary>
    /// The vertical amount, range 0 - 1 where
    /// 0   = All up
    /// 0.5 = Middel
    /// 1   = All down
    /// </summary>
    double Y { get; }


    public Vector2 LeftStickInput =>
        new Vector2((float)(X - 0.5) * 2, (float)(Y - 0.5) * 2);

    GamepadButton StartButton { get; }

    GamepadButton SelectButton { get; }

    GamepadButton NorthButton { get; }

    GamepadButton EastButton { get; }

    GamepadButton SouthButton { get; }

    GamepadButton WestButton { get; }
}
