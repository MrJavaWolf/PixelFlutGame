using PixelFlut.Core;
using PixelFlut.StickFigure;

namespace StickFigureGame;

public enum StickFigureAnimation
{
    Idle,
    Shoot,
    Run,
    JumpTop,
    JumpUp,
    JumpDown,
    SwordAttack,
    Dash,
    TakeDamage,
}

public class StickFigureAnimator
{
    private const string BasePath = "StickFigure/Resources/spritesheet/";
    private const string PlayerIdle = BasePath + "Player/Character Idle 48x48.png";
    private const string PlayerShoot = BasePath + "Player/player shoot 2H 48x48.png";
    private const string PlayerRun = BasePath + "Player/Player Sword Run 48x48.png";
    private const string PlayerJumpUp = BasePath + "Player/player jump 48x48.png";
    private const string PlayerJumpTop = BasePath + "Player/player jump 48x48.png";
    private const string PlayerJumpDown = BasePath + "Player/player jump 48x48.png";
    private const string PlayerSwordAttack = BasePath + "Player/player sword atk 64x64.png";
    private const string PlayerDash = BasePath + "Player/Player Roll 48x48.png";
    private const string PlayerTakeDamage = BasePath + "Player/player air spin 48x48.png";

    private StickFigureBase stickFigureBase;
    private SpriteAnimation idle;
    private SpriteAnimation shoot;
    private SpriteAnimation run;
    private SpriteAnimation jumpTop;
    private SpriteAnimation jumpUp;
    private SpriteAnimation jumpDown;
    private SpriteAnimation swordAttack;
    private SpriteAnimation dash;
    private SpriteAnimation takeDamage;

    private SpriteAnimation currentAnimation;

    public bool FlipX { get; set; }
    private bool restartAnimation = false;

    public StickFigureAnimator(
        StickFigureBase stickFigureBase,
        SpriteLoader spriteLoader)
    {
        this.stickFigureBase = stickFigureBase;
        idle = spriteLoader.LoadAnimation(PlayerIdle, 48, 48, 24, TimeSpan.FromMilliseconds(100));
        shoot = spriteLoader.LoadAnimation(PlayerShoot, 48, 48, 24, TimeSpan.FromMilliseconds(100));
        run = spriteLoader.LoadAnimation(PlayerRun, 48, 48, 24, TimeSpan.FromMilliseconds(100));
        jumpUp = spriteLoader.LoadAnimation(PlayerJumpUp, 48, 48, 24, animation: new() { 0 });
        jumpTop = spriteLoader.LoadAnimation(PlayerJumpTop, 48, 48, 24, animation: new() { 1 });
        jumpDown = spriteLoader.LoadAnimation(PlayerJumpDown, 48, 48, 24, animation: new() { 2 });
        swordAttack = spriteLoader.LoadAnimation(PlayerSwordAttack, 64, 64, 32, TimeSpan.FromMilliseconds(100));
        dash = spriteLoader.LoadAnimation(PlayerDash, 48, 48, 24, animation: new() { 1 });
        takeDamage = spriteLoader.LoadAnimation(PlayerTakeDamage, 48, 48, 24, animation: new() { 4 });
        currentAnimation = idle;
    }


    public void Play(StickFigureAnimation animation)
    {
        switch (animation)
        {
            case StickFigureAnimation.Dash: SetAnimation(dash); break;
            case StickFigureAnimation.Idle: SetAnimation(idle); break;
            case StickFigureAnimation.JumpDown: SetAnimation(jumpDown); break;
            case StickFigureAnimation.JumpTop: SetAnimation(jumpTop); break;
            case StickFigureAnimation.JumpUp: SetAnimation(jumpUp); break;
            case StickFigureAnimation.Run: SetAnimation(run); break;
            case StickFigureAnimation.Shoot: SetAnimation(shoot); break;
            case StickFigureAnimation.SwordAttack: SetAnimation(swordAttack); break;
            case StickFigureAnimation.TakeDamage: SetAnimation(takeDamage); break;
            default: SetAnimation(idle); break;
        }
    }

    private void SetAnimation(SpriteAnimation animation)
    {
        if (animation != currentAnimation)
        {
            restartAnimation = true;
        }
        currentAnimation = animation;
    }

    public List<PixelBuffer> Render(GameTime time)
    {
        if (restartAnimation)
        {
            currentAnimation.Restart(time);
            restartAnimation = false;
        }
        currentAnimation.FlipX = stickFigureBase.Facing != StickFigureBase.FacingDirection.Right;
        currentAnimation.SetPosition(stickFigureBase.Position);
        return currentAnimation.Render(time);
    }

    //public PixelBuffer Render(PixelBufferFactory pixelBufferFactory, float renderScale)
    //{
    //    int playerBorder = 2;
    //    int xSize = (int)(renderScale * stickFigureBase.Size.X) + playerBorder * 2;
    //    int ySize = (int)(renderScale * stickFigureBase.Size.Y) + playerBorder * 2;
    //    int numberOfPixels = xSize * ySize;

    //    PixelBuffer buffer = pixelBufferFactory.Create(numberOfPixels);
    //    for (int x = 0; x < xSize; x++)
    //    {
    //        int xPos = (int)(stickFigureBase.Position.X * renderScale) + x;
    //        for (int y = 0; y < ySize; y++)
    //        {
    //            int yPos = (int)(stickFigureBase.Position.Y * renderScale) + y;
    //            if (x > playerBorder && x < xSize - playerBorder &&
    //                y > playerBorder && y < ySize - playerBorder)
    //            {
    //                buffer.SetPixel(x * ySize + y, xPos, yPos, Color.Pink);
    //            }
    //            else
    //            {
    //                buffer.SetPixel(x * ySize + y, xPos, yPos, Color.Black);
    //            }
    //        }
    //    }
    //    return buffer;
    //}
}
