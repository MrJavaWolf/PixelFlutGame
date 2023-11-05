using PixelFlut.Core;
using PixelFlut.Core.Sprite;
using System.Numerics;

namespace StickFigureGame;


public class StickFigureSlashAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string SlashSheet = BasePath + "Effect/Slash/pngfind.com-rpg-png-887284.png";
    private SpriteAnimation animation;

    private int pixelsPerUnit = 70;
    private int spriteWidth = 188;
    private int spriteHeight = 188;

    public StickFigureSlashAnimator(SpriteLoader spriteLoader)
    {
        animation = spriteLoader.LoadAnimation(SlashSheet, spriteWidth, spriteHeight, pixelsPerUnit, TimeSpan.FromMilliseconds(50), 
            new List<int>() { 3, 4, 6 },
            loopAnimation: false);
    }

    public void Play(float angle, Vector2 centerPosition, bool flipY, GameTime time)
    {
        animation.Restart(time);
        animation.FlipY = flipY;
        animation.SetRotation(angle);
        // The animation wants lower left conor, so we calculate the center to lower left offset
        Vector2 offset = new Vector2(spriteWidth / 2.0f / pixelsPerUnit, spriteHeight / 2.0f / pixelsPerUnit);
        animation.SetPosition(centerPosition - offset);
    }

    public List<PixelBuffer> Loop(GameTime time)
    {
       return animation.Render(time);
    }
}
