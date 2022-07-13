using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PixelFlut.Core;
using PixelFlut.Images;
using PixelFlut.Pong;
using PixelFlut.TestImage;
using Serilog;
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
        services.AddSingleton(configuration.GetSection("Screen").Get<PixelFlutScreenConfiguration>());
        services.AddSingleton(configuration.GetSection("Gamepad").Get<PixelFlutGamepadConfiguration>());
        services.AddSingleton(configuration.GetSection("GameLoop").Get<GameLoopConfiguration>());
        services.AddSingleton<IPixelFlutScreenProtocol, PixelFlutScreenProtocol1>();
        services.AddSingleton<GamePadsController>();
        services.AddSingleton<PixelBufferFactory>();
        services.AddSingleton<ConsoleAsGamePad>();
        services.AddSingleton<GameLoop>();
        services.AddTransient<PixelFlutScreen>();
        services.AddTransient<GameSelector>();
        services.AddSingleton(new StoppingToken(tokenSource.Token));
        services.AddHttpClient();
        services.AddSingleton(gameFactory);
        AddGames(services, gameFactory);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

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
}


