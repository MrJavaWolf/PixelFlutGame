using DevDecoder.HIDDevices.Controllers;
using DynamicData;
using PixelFlut.Core;
using PixelFlut.Pong;
using System.Numerics;

namespace StickFigureGame;

public class StickFigureGameConfiguration
{

}

public class StickFigureGame : IGame
{
    private readonly StickFigureGameConfiguration config;
    private readonly IPixelFlutScreenProtocol screenProtocol;
    private readonly ILogger<StickFigureGame> logger;
    private List<PixelBuffer> pixelBuffers = new();
    private StickFigureWorld world;
    private List<StickFigureCharacterController> Players = new();

    public StickFigureGame(
        StickFigureGameConfiguration config, 
        IPixelFlutScreenProtocol screenProtocol, 
        ILogger<StickFigureGame> logger)
    {
        this.config = config;
        this.screenProtocol = screenProtocol;
        this.logger = logger;
        StickFigureWorldData stickFigureWorldData = StickFigureWorldImporter.LoadWorldData();
        world = new StickFigureWorld(stickFigureWorldData);
    }

    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        // If a game pad have been disconnected
        while (world.Players.Count > gamePads.Count)
        {
            world.Players.RemoveAt(world.Players.Count - 1);
        }

        // If a game pad have been connected
        while (world.Players.Count < gamePads.Count)
        {
            SpawnPlayers();
        }

        // Game logic
        for (int i = 0; i < gamePads.Count; i++)
        {
            world.Players[i].Loop(time, gamePads[i]);
        }


        // Render

        return pixelBuffers;
    }

    void SpawnPlayers()
    {
        Vector2 spawnPoint = world.SpawnPoints[Random.Shared.Next(world.SpawnPoints.Count)];
        Players.Add(new StickFigureCharacterController(world, spawnPoint, logger));
    }
}
