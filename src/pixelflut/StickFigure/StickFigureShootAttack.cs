using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;


public class StickFigureShootAttack
{
    public float AttackStartSpeed = -5f;
    public float AttackEndSpeed = -2f;
    public float AttackDuration = 0.3f;
    public float AttackCooldown = 0.4f;
    public float ShootDelay = 0f;

    private double startAttackTime = -1f;
    private Vector2 pushBackDirection = Vector2.Zero;
    private Vector2 shootDirection = Vector2.Zero;
    private bool HaveShoot = false;

    private StickFigureBase stickFigureBase;
    private StickFigureWorld world;
    private StickFigureCharacterController player;
    private readonly ObjectPool<StickFigureProjectileAnimator> projectileAnimators;
    private readonly ObjectPool<StickFigureExplosionEffectAnimator> explosionAnimators;

    public StickFigureShootAttack(
        StickFigureBase stickFigureBase,
        StickFigureWorld world,
        StickFigureCharacterController player,
        ObjectPool<StickFigureProjectileAnimator> projectileAnimators,
        ObjectPool<StickFigureExplosionEffectAnimator> explosionAnimators)
    {
        this.stickFigureBase = stickFigureBase;
        this.world = world;
        this.player = player;
        this.projectileAnimators = projectileAnimators;
        this.explosionAnimators = explosionAnimators;
    }

    public bool CanStartAttack(GameTime time) => time.TotalTime.TotalSeconds - startAttackTime - AttackDuration > AttackCooldown;

    public void StartAttack(GameTime time, IGamePadDevice gamePad)
    {
        startAttackTime = time.TotalTime.TotalSeconds;
        Vector2 input = gamePad.LeftStickInput;
        pushBackDirection = GetPushBackDirection(input);
        shootDirection = GetShootDirection(input);
        HaveShoot = false;
        stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.Shoot);
    }

    public void Interrupt()
    {
        startAttackTime = -1;
    }

    private Vector2 GetShootDirection(Vector2 input)
    {
        if (input == Vector2.Zero)
        {
            return stickFigureBase.Facing == StickFigureBase.FacingDirection.Left ?
                Vector2.UnitX * -1 :
                Vector2.UnitX;
        }
        else
        {
            return Vector2.Normalize(input);
        }
    }

    private Vector2 GetPushBackDirection(Vector2 input)
    {
        if (input != Vector2.Zero)
        {
            Vector2 inputDirection = Vector2.Normalize(input);
            return inputDirection;
        }
        else
        {
            return stickFigureBase.Facing == StickFigureBase.FacingDirection.Left ?
               Vector2.UnitX * -1 :
               Vector2.UnitX;
        }
    }

    public bool IsAttacking(GameTime time) => time.TotalTime.TotalSeconds - startAttackTime <= AttackDuration;

    // Update is called once per frame
    public void Loop(GameTime time)
    {
        float ratio = (float)(time.TotalTime.TotalSeconds - startAttackTime) / AttackDuration;
        var currentSpeed = AttackStartSpeed + (AttackEndSpeed - AttackStartSpeed) * ratio;
        stickFigureBase.Velocity = pushBackDirection * currentSpeed;

        if (time.TotalTime.TotalSeconds - startAttackTime > ShootDelay && !HaveShoot)
        {
            HaveShoot = true;
            SpawnProjectile(time);
        }
    }

    private void SpawnProjectile(GameTime time)
    {
        Vector2 spawnPosition = player.Center;
        StickFigureProjectile proj = new StickFigureProjectile(world, projectileAnimators, explosionAnimators);
        proj.DoStart(time, shootDirection, spawnPosition, player);
    }
}
