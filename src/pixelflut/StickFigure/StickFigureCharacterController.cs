using Humper;
using PixelFlut.Core;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureCharacterController
{
    public Vector2 Center =>
        new Vector2(stickFigureBase.Position.X, stickFigureBase.Position.Y) +
        new Vector2(stickFigureBase.Size.X / 2, stickFigureBase.Size.Y / 2);

    public IBox Box => stickFigureBase.box;
    private StickFigureJump jump;
    private StickFigureMovement movement;
    private StickFigureDash dash;
    private StickFigureBase stickFigureBase;
    private StickFigureSlashAttack slashAttack;
    private StickFigureShootAttack shootAttack;
    private StickFigureWorld world;
    private StickFigureTakingDamage takingDamage;
    private double TimeOfDeath = -1;
    private float RespawnLockTime = 1;
    private float RespawnInvulnerableTime = 2;

    public StickFigureCharacterController(StickFigureWorld world, Vector2 spawnPoint)
    {
        this.world = world;
        stickFigureBase = new StickFigureBase(world, spawnPoint);
        jump = new StickFigureJump(stickFigureBase);
        movement = new StickFigureMovement(stickFigureBase);
        dash = new StickFigureDash(stickFigureBase);
        slashAttack = new StickFigureSlashAttack(stickFigureBase, world);
        shootAttack = new StickFigureShootAttack(stickFigureBase, world, this);
        takingDamage = new StickFigureTakingDamage(stickFigureBase);
        world.Players.Add(this);
    }


    void Loop(GameTime time)
    {
        if (time.TotalTime.TotalSeconds - TimeOfDeath < RespawnLockTime)
        {
            stickFigureBase.Velocity = Vector2.Zero;
            return;
        }

        if (this.Center.Y < -20)
        {
            TimeOfDeath = time.TotalTime.TotalSeconds;
            dash.Interrupt();
            shootAttack.Interrupt();
            slashAttack.Interrupt();
            takingDamage.Interrupt();
            StickFigureRespawnPointExport spawnPosition = world.SpawnPoints[Random.Shared.Next(0, world.SpawnPoints.Count)];
            stickFigureBase.Teleport(new Vector2(spawnPosition.X, spawnPosition.Y));
        }

        if (!shootAttack.IsAttacking(time) &&
            !dash.IsDashing(time) &&
            !takingDamage.IsTakingDamage() &&
            stickFigureBase.Input.GetSlashAttackInputOnDown() &&
            slashAttack.CanStartAttack(time))
        {
            slashAttack.StartAttack(this, time);
        }

        if (!slashAttack.IsAttacking(time) &&
            !dash.IsDashing(time) &&
            !takingDamage.IsTakingDamage() &&
            stickFigureBase.Input.GetShootAttackInput() && shootAttack.CanStartAttack(time))
        {
            shootAttack.StartAttack(time);
        }

        if (!slashAttack.IsAttacking(time) &&
            !shootAttack.IsAttacking(time) &&
            !takingDamage.IsTakingDamage() &&
            stickFigureBase.Input.GetDashInput() && dash.CanStartDash(time))
        {
            dash.StartDash(time);
        }

        if (takingDamage.IsTakingDamage()) takingDamage.Loop();
        else if (slashAttack.IsAttacking(time)) slashAttack.Loop(time);
        else if (shootAttack.IsAttacking(time)) shootAttack.Loop(time);
        else if (dash.IsDashing(time))
        {
            dash.Loop();
        }
        else
        {
            jump.Loop(time);
            movement.Loop(time);
        }
    }

    public void TakeDamage(Vector2 damagePushback, GameTime time)
    {
        // Do not take damange when you respawn
        if (time.TotalTime.TotalSeconds - TimeOfDeath < RespawnLockTime + RespawnInvulnerableTime)
            return;

        dash.Interrupt();
        shootAttack.Interrupt();
        slashAttack.Interrupt();
        takingDamage.StartTakeDamage(damagePushback);
    }
}
