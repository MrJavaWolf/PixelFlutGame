using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Numerics;

namespace StickFigureGame;


public class StickFigureSlashAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string SlashSheet = BasePath + "Effect/Slash/pngfind.com-rpg-png-887284.png";
    private SpriteAnimation animation;

    public StickFigureSlashAnimator(SpriteLoader spriteLoader)
    {
        animation = spriteLoader.LoadAnimation(SlashSheet, 189, 189, TimeSpan.FromMilliseconds(75), 
            new List<int>() { 3, 4, 5, 6 },
            loopAnimation: false);
    }

    public void Play(float angle, Vector2 position, bool flipY, GameTime time)
    {
        animation.Restart(time);
        animation.FlipY = flipY;
        animation.SetRotation(angle);
        animation.SetPosition(position);
    }

    public List<PixelBuffer> Loop(GameTime time)
    {
       return animation.Render(time);
    }
}
