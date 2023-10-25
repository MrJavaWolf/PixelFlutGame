using System.Numerics;

namespace PixelFlut.Core.Sprite;


public class SpriteAnimation
{
    private static readonly List<PixelBuffer> empty = new List<PixelBuffer>();

    private List<SpriteFrame> frames;

    private int animationIndex = 0;

    private TimeSpan nextFrameTime = TimeSpan.Zero;

    private IReadOnlyList<int> animation;

    public TimeSpan TimeBetweenFrames { get; set; }

    public bool LoopAnimation { get; set; }

    public bool FlipX
    {
        get => frames[animation[animationIndex]].FlipX;
        set => frames[animation[animationIndex]].FlipX = value;
    }

    public bool FlipY
    {
        get => frames[animation[animationIndex]].FlipY;
        set => frames[animation[animationIndex]].FlipY = value;
    }

    public SpriteAnimation(
        List<SpriteFrame> frames,
        TimeSpan timeBetweenFrames,
        List<int>? animation = null,
        bool loopAnimation = true)
    {
        this.frames = frames;
        TimeBetweenFrames = timeBetweenFrames;
        LoopAnimation = loopAnimation;

        if (animation != null) this.animation = animation;
        else this.animation = Enumerable.Range(0, frames.Count).ToList();
        if (this.animation.Count == 0) throw new Exception("Failed create sprite animation, animation lenght is 0");
    }

    public void Restart(GameTime time)
    {
        nextFrameTime = time.TotalTime + TimeBetweenFrames;
    }

    public bool IsAnimationDone(GameTime time) =>
        !LoopAnimation &&
        time.TotalTime > nextFrameTime &&
        animationIndex == animation.Count - 1;

    private bool ShouldGoToNextFrame(GameTime time) =>
        animation.Count > 1 &&                                      // Only change frame if we have more than 1 frame
        time.TotalTime > nextFrameTime &&                           // Change frame when it is time to change frame
        (LoopAnimation || animationIndex != animation.Count - 1);   // Only change frame we we are not on the last frame, or should loop the frame

    public List<PixelBuffer> Render(GameTime time)
    {
        // Checks if it is an animation
        if (ShouldGoToNextFrame(time))
        {
            SpriteFrame previousFrame = frames[animation[animationIndex]];

            // Renders next frame
            animationIndex++;
            if (animationIndex >= animation.Count)
            {
                if (LoopAnimation)
                    animationIndex = 0;
                else
                    animationIndex--;
            }
            UpdateAnimationIndex(
                animationIndex,
                time,
                previousFrame.Position,
                previousFrame.FlipX,
                previousFrame.FlipY,
                previousFrame.Rotation);

        }
        if (IsAnimationDone(time))
            return empty;
        else
            return new List<PixelBuffer>() { frames[animation[animationIndex]].Buffer };
    }

    public void SetPosition(Vector2 position)
    {
        frames[animation[animationIndex]].SetPosition(position);
    }

    public void SetRotation(float rotation)
    {
        frames[animation[animationIndex]].SetRotation(rotation);
    }

    public void SetAnimation(
        GameTime time,
        List<int> animation,
        int startAnimationIndex = 0,
        TimeSpan? timeBetweenFrames = null,
        bool loopAnimation = true)
    {
        SpriteFrame previousFrame = frames[animation[animationIndex]];
        LoopAnimation = loopAnimation;
        this.animation = animation;
        TimeBetweenFrames = timeBetweenFrames ?? TimeBetweenFrames;

        UpdateAnimationIndex(
            startAnimationIndex,
            time,
            previousFrame.Position,
            previousFrame.FlipX,
            previousFrame.FlipY,
            previousFrame.Rotation);
    }

    public void UpdateAnimationIndex(
        int toIndex,
        GameTime time,
        Vector2 position,
        bool flipX,
        bool flipY,
        float rotation)
    {
        frames[animation[toIndex]].FlipX = flipX;
        frames[animation[toIndex]].FlipY = flipY;
        frames[animation[toIndex]].SetRotation(rotation);
        frames[animationIndex].SetPosition(position);

        animationIndex = toIndex;
        nextFrameTime = time.TotalTime + TimeBetweenFrames;
    }
}
