using PixelFlut.Core;
using PixelFlut.StickFigure;
using System.Net.Http;
using System.Numerics;

namespace StickFigureGame;

public class StickFigureGameConfiguration
{
    public float PlayerViewSphere { get; set; }
    public float RenderScale { get; set; }
}

public class StickFigureGame : IGame
{
    private readonly StickFigureGameConfiguration config;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly PixelBufferFactory pixelBufferFactory;
    private readonly IPixelFlutScreenProtocol screenProtocol;

    private readonly ILogger<StickFigureGame> logger;
    private readonly SpriteLoader spriteLoader;
    private StickFigureWorld world;
    private List<StickFigureCharacterController> Players = new();
    private StickFigureWorldRenderer renderer;

    public StickFigureGame(
        StickFigureGameConfiguration config,
        IHttpClientFactory httpClientFactory,
        PixelBufferFactory pixelBufferFactory,
        IPixelFlutScreenProtocol screenProtocol,
        ILogger<StickFigureGame> logger,
        SpriteLoader spriteLoader)
    {
        this.config = config;
        this.httpClientFactory = httpClientFactory;
        this.pixelBufferFactory = pixelBufferFactory;
        this.screenProtocol = screenProtocol;
        this.logger = logger;
        this.spriteLoader = spriteLoader;
        StickFigureWorldData stickFigureWorldData = StickFigureWorldImporter.LoadWorldData();
        world = new StickFigureWorld(stickFigureWorldData);
        renderer = new StickFigureWorldRenderer(config, screenProtocol, world, pixelBufferFactory);
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
        List<PixelBuffer> buffer = renderer.Render(time);
        return buffer;
    }

    void SpawnPlayers()
    {
        Vector2 spawnPoint = world.SpawnPoints[Random.Shared.Next(world.SpawnPoints.Count)];
        Players.Add(new StickFigureCharacterController(world, spawnPoint, logger, screenProtocol, spriteLoader));
    }
}
