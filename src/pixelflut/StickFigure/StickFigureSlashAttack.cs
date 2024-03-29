using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;


public class StickFigureSlashAttack
{
    public float AttackStartSpeed = 0f;
    public float AttackEndSpeed = 0f;
    public float AttackDuration = 0.25f;
    public float AttackCooldown = 0.25f;
    public float AttackDamageWindowDuration = 0.1f;
    public float DamageRadius = 2f;
    public float DamageAngle = 80f;
    public float DamageForce = 12f;

    private double startAttackTime = -1f;
    private Vector2 attackDirection = Vector2.Zero;
    public float slashEffectAngleOffset = 0f;

    private StickFigureBase stickFigureBase;
    private List<StickFigureCharacterController> playerHits = new List<StickFigureCharacterController>();
    private List<StickFigureProjectile> projetileHits = new List<StickFigureProjectile>();
    private StickFigureWorld world;
    private StickFigureCharacterController? player;
    public StickFigureSlashAnimator SlashAnimator;


    public StickFigureSlashAttack(StickFigureBase stickFigureBase, StickFigureWorld world, StickFigureSlashAnimator slashAnimator)
    {
        SlashAnimator = slashAnimator;
        this.stickFigureBase = stickFigureBase;
        this.world = world;
    }

    public bool CanStartAttack(GameTime time) => time.TotalTime.TotalSeconds - startAttackTime - AttackDuration > AttackCooldown;

    public void Interrupt()
    {
        startAttackTime = -1;
    }

    public void StartAttack(StickFigureCharacterController player, GameTime time, IGamePadDevice gamePad)
    {
        this.player = player;
        playerHits.Clear();
        startAttackTime = time.TotalTime.TotalSeconds;
        Vector2 input = gamePad.LeftStickInput;
        attackDirection = GetAttackDirection(input);
        stickFigureBase.Facing = attackDirection.X > 0 ?
              StickFigureBase.FacingDirection.Right :
              StickFigureBase.FacingDirection.Left;
        stickFigureBase.PlayerAnimator.FlipX = stickFigureBase.Facing == StickFigureBase.FacingDirection.Left;
        stickFigureBase.PlayerAnimator.Play(StickFigureAnimation.SwordAttack);
        float angle = Vector2.UnitX.SignedAngle(attackDirection);
        //bool flipY = stickFigureBase.Facing == StickFigureBase.FacingDirection.Left ? false : true;
        bool flipY = false;
        SlashAnimator.Play(angle + this.slashEffectAngleOffset, player.Center, flipY, time);
    }

    private Vector2 GetAttackDirection(Vector2 input)
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
        stickFigureBase.Velocity = attackDirection * currentSpeed;

        ReflectProjectile(time);
        if (time.TotalTime.TotalSeconds - startAttackTime < AttackDamageWindowDuration)
        {
            CheckHit(time);
        }
    }


    private void CheckHit(GameTime time)
    {
        if (player == null) return;
        foreach (StickFigureCharacterController enemy in world.Players)
        {
            if (enemy == player) continue;
            Vector2 directionToEnemy = Vector2.Normalize(enemy.Center - player.Center);
            if (Vector2.Distance(enemy.Center, player.Center) > DamageRadius) continue;

            float angleToEnemy = MathF.Abs(attackDirection.SignedAngle(directionToEnemy));

            if (angleToEnemy <= DamageAngle)
            {
                if (!playerHits.Contains(enemy))
                {
                    playerHits.Add(enemy);
                    OnEnemyHit(enemy, directionToEnemy, time);
                }
            }
        }
    }

    private void OnEnemyHit(StickFigureCharacterController enemy, Vector2 directionToEnemy, GameTime time)
    {
        enemy.TakeDamage(directionToEnemy * DamageForce, time);
    }

    private void ReflectProjectile(GameTime time)
    {
        if (player == null) return;
        foreach (StickFigureProjectile projetile in world.Projectiles)
        {
            Vector2 directionToProjetile = Vector2.Normalize(projetile.CenterPosition - player.Center);
            if (Vector2.Distance(projetile.CenterPosition, player.Center) > DamageRadius) continue;

            float angleToEnemy = MathF.Abs(attackDirection.SignedAngle(directionToProjetile));

            if (angleToEnemy <= DamageAngle)
            {
                if (!projetileHits.Contains(projetile))
                {
                    projetileHits.Add(projetile);
                    OnProjetileHit(projetile, directionToProjetile, time);
                }
            }
        }
    }

    private void OnProjetileHit(StickFigureProjectile projetile, Vector2 directionToProjetile, GameTime time)
    {
        if (player == null) return;
        projetile.DoStart(time, directionToProjetile, projetile.CenterPosition, player);
    }
}
