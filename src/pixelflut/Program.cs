using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using PixelFlut.Core;
using PixelFlut.Core.Sprite;
using PixelFlut.Distributed;
using PixelFlut.Images;
using PixelFlut.Pong;
using PixelFlut.Snake;
using PixelFlut.TestImage;
using Serilog;
using StickFigureGame;
using System.Text;

namespace PixelFlut;

public class Program
{
    /// <summary>
    /// Register your games here
    /// The name of your game will be the same you will be using in the configuration file
    /// Both for your game configuration and for selecting the game in 'GameLoop --> GameToPlay'
    /// </summary>
    private static void AddGames(ServiceCollection services, GameFactory gameFactory)
    {
        // Add a game by either
        // gameFactory.AddGame<YourGameClass>("YourGameName", services);
        // gameFactory.AddGame<YourGameClass, YourGameConfigClass>("YourGameName", services);

        gameFactory.AddGame<BlackTestImage>("BlackTestImage", services);
        gameFactory.AddGame<RainbowTestImage, RainbowTestImage.Configuration>("RainbowTestImage", services);
        gameFactory.AddGame<GameImage, GameImage.Configuration>("Image", services);
        gameFactory.AddGame<PongGame, PongConfiguration>("Pong", services);
        gameFactory.AddGame<SnakeGame, SnakeConfiguration>("Snake", services);
        gameFactory.AddGame<DistributedWorker, DistributedWorkerConfiguration>("Distributed", services);
        gameFactory.AddGame<StickFigureGame.StickFigureGame, StickFigureGameConfiguration>("StickFigure", services);
    }

    public static async Task Main(string[] args)
    {
        // Configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Setup gracefull shutdown
        CancellationTokenSource tokenSource = new();
        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            tokenSource.Cancel();
        };

        // Dependency injection
        GameFactory gameFactory = new GameFactory(configuration);
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger()));
        services.AddSingleton(configuration);
        services.AddSingleton(Read<PixelFlutScreenConfiguration>(configuration, "Screen"));
        services.AddSingleton(Read<PixelFlutGamepadConfiguration>(configuration, "Gamepad"));
        services.AddSingleton(Read<GameLoopConfiguration>(configuration, "GameLoop"));
        services.AddSingleton(Read<DistributedServerConfiguration>(configuration, "DistributedServer"));
        services.AddSingleton(Read<MqttGameChangerConfiguration>(configuration, "Mqtt"));
        services.AddSingleton<ObjectPool<StickFigureProjectileAnimator>>(serviceProvider =>
        {
            return new DefaultObjectPool<StickFigureProjectileAnimator>(
                 new StickFigureProjectileAnimatorPooledObjectPolicy(serviceProvider.GetRequiredService<SpriteLoader>()));
        });
        services.AddSingleton<ObjectPool<StickFigureExplosionEffectAnimator>>(serviceProvider =>
        {
            return new DefaultObjectPool<StickFigureExplosionEffectAnimator>(
                 new StickFigureExplosionEffectAnimatorPooledObjectPolicy(serviceProvider.GetRequiredService<SpriteLoader>()));
        });
        services.AddSingleton<IPixelFlutScreenProtocol, PixelFlutScreenProtocol0>();
        services.AddSingleton<MqttGameChanger>();
        services.AddSingleton<GamePadsController>();
        services.AddSingleton<DistributedServer>();
        services.AddSingleton<PixelBufferFactory>();
        services.AddSingleton<ConsoleAsGamePad>();
        services.AddSingleton<GameLoop>();
        services.AddSingleton<PixelFlutScreen>();
        services.AddSingleton<SpriteLoader>();
        services.AddTransient<GameSelector>();
        services.AddSingleton(new StoppingToken(tokenSource.Token));
        services.AddHttpClient();
        services.AddSingleton(gameFactory);
        AddGames(services, gameFactory);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Create game loop
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"- - - - - Starting pixelflut game - - - - - ");
        GamePadsController gamepadsController = serviceProvider.GetRequiredService<GamePadsController>();
        GameLoop gameLoop = serviceProvider.GetRequiredService<GameLoop>();

        // Run
        Task t1 = Task.Run(async () => await gamepadsController.RunAsync(tokenSource.Token));
        Thread gameLoopThread = new(() => gameLoop.Run(tokenSource.Token));
        gameLoopThread.Start();
        gameLoopThread.Join();
        await Task.WhenAll(t1);

        logger.LogInformation($"- - - - -  Shutdown pixelflut game - - - - - ");
    }

    private static T Read<T>(IConfiguration configuration, string conf)
    {
        return configuration.GetRequiredSection(conf).Get<T>() ??
            throw new Exception($"Failed to read configuration: {conf}");
    }
}


