using Humper;
using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Numerics;
namespace StickFigureGame;

public class StickFigureCharacterController
{
    public Vector2 Center =>
        new Vector2(StickFigureBase.Position.X, StickFigureBase.Position.Y) +
        new Vector2(StickFigureBase.Size.X / 2, StickFigureBase.Size.Y / 2);
    
    public StickFigureBase StickFigureBase { get; private set; }
    public StickFigureSlashAnimator SlashAnimator { get; private set; }

    public IBox Box => StickFigureBase.box;
    private StickFigureJump jump;
    private StickFigureMovement movement;
    private StickFigureDash dash;
    private StickFigureSlashAttack slashAttack;
    private StickFigureShootAttack shootAttack;
    private StickFigureWorld world;
    private readonly ILogger logger;
    private StickFigureTakingDamage takingDamage;
    private double TimeOfDeath = -1;
    private float RespawnLockTime = 1;
    private float RespawnInvulnerableTime = 2;

    public StickFigureCharacterController(
        StickFigureWorld world, 
        Vector2 spawnPoint, 
        ILogger logger, 
        IPixelFlutScreenProtocol screenProtocol,
        SpriteLoader spriteLoader)
    {
        this.world = world;
        this.logger = logger;
        StickFigureBase = new StickFigureBase(world, spawnPoint, spriteLoader);
        jump = new StickFigureJump(StickFigureBase);
        movement = new StickFigureMovement(StickFigureBase);
        dash = new StickFigureDash(StickFigureBase);
        SlashAnimator = new StickFigureSlashAnimator(spriteLoader);
        slashAttack = new StickFigureSlashAttack(StickFigureBase, world, SlashAnimator);
        shootAttack = new StickFigureShootAttack(StickFigureBase, world, this);
        takingDamage = new StickFigureTakingDamage(StickFigureBase);
        world.Players.Add(this);
    }


    public void Loop(GameTime time, IGamePadDevice gamePad)
    {
        if (time.TotalTime.TotalSeconds - TimeOfDeath < RespawnLockTime)
        {
            StickFigureBase.Velocity = Vector2.Zero;
            StickFigureBase.Loop(time);
            return;
        }

        if (this.Center.Y < -20)
        {
            TimeOfDeath = time.TotalTime.TotalSeconds;
            dash.Interrupt();
            shootAttack.Interrupt();
            slashAttack.Interrupt();
            takingDamage.Interrupt();
            Vector2 spawnPosition = world.SpawnPoints[Random.Shared.Next(0, world.SpawnPoints.Count)];
            StickFigureBase.Teleport(spawnPosition);
            logger.LogInformation("Player fell off the map, respawns the player");
        }

        if (!shootAttack.IsAttacking(time) &&
            !dash.IsDashing(time) &&
            !takingDamage.IsTakingDamage(time) &&
            gamePad.WestButton.OnPress &&
            slashAttack.CanStartAttack(time))
        {
            logger.LogInformation("Player attack");
            slashAttack.StartAttack(this, time, gamePad);
        }

        if (!slashAttack.IsAttacking(time) &&
            !dash.IsDashing(time) &&
            !takingDamage.IsTakingDamage(time) &&
            gamePad.NorthButton.OnPress &&
            shootAttack.CanStartAttack(time))
        {
            logger.LogInformation("Player shoot");
            shootAttack.StartAttack(time, gamePad);
        }

        if (!slashAttack.IsAttacking(time) &&
            !shootAttack.IsAttacking(time) &&
            !takingDamage.IsTakingDamage(time) &&
            gamePad.EastButton.OnPress &&
            dash.CanStartDash(time))
        {
            logger.LogInformation("Player dash");
            dash.StartDash(time, gamePad);
        }

        if (takingDamage.IsTakingDamage(time)) takingDamage.Loop(time);
        else if (slashAttack.IsAttacking(time)) slashAttack.Loop(time);
        else if (shootAttack.IsAttacking(time)) shootAttack.Loop(time);
        else if (dash.IsDashing(time)) dash.Loop();
        else
        {
            jump.Loop(time, gamePad);
            movement.Loop(time, gamePad);
        }

        StickFigureBase.Loop(time);
    }

    public void TakeDamage(Vector2 damagePushback, GameTime time)
    {
        // Do not take damange when you respawn
        if (time.TotalTime.TotalSeconds - TimeOfDeath < RespawnLockTime + RespawnInvulnerableTime)
            return;

        dash.Interrupt();
        shootAttack.Interrupt();
        slashAttack.Interrupt();
        takingDamage.StartTakeDamage(time, damagePushback);
        logger.LogInformation("Player take damage");

    }
}
