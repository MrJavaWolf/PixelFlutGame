using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Numerics;

namespace StickFigureGame;


public class StickFigureSlashAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string SlashSheet = BasePath + "Effect/Slash/pngfind.com-rpg-png-887284.png";
    private readonly SpriteLoader spriteLoader;

    private SpriteAnimation slash;

    public StickFigureSlashAnimator(SpriteLoader spriteLoader)
    {
        this.spriteLoader = spriteLoader;
        slash = spriteLoader.LoadAnimation(SlashSheet, 189, 189, TimeSpan.FromMilliseconds(75), 
            new List<int>() { 3, 4, 5, 6 },
            loopAnimation: false);

    }

    public void Play(float angle, Vector2 position, bool flipY, GameTime time)
    {
        slash.Restart(time);
        slash.FlipY = flipY;
        slash.SetPosition(position);
    }

    public List<PixelBuffer> Loop(GameTime time)
    {
       return slash.Render(time);
    }

}
