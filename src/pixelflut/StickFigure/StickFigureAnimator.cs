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
    public bool FlipX { get; set; }
    
    public StickFigureAnimator()
    {

    }



    public void Play(StickFigureAnimation animation)
    {

    }
}
