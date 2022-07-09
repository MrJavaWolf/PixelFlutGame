
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PixelFlut.Core;
using PixelFlut.Pong;
using PixelFlut.TestImage;
using Serilog;
namespace PixelFlut;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configuration
        IConfiguration Configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args)
                        .Build();
        PixelFlutScreenConfiguration rendererConfig = Configuration.GetSection("Screen").Get<PixelFlutScreenConfiguration>();
        PixelFlutGamepadConfiguration gamepadConfig = Configuration.GetSection("Gamepad").Get<PixelFlutGamepadConfiguration>();
        GameLoopConfiguration gameloopConfig = Configuration.GetSection("GameLoop").Get<GameLoopConfiguration>();

        


        // Dependency injection
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddSingleton(rendererConfig);
        services.AddSingleton(gamepadConfig);
        services.AddSingleton(gameloopConfig);

        services.AddSingleton<IPixelFlutScreenProtocol, PixelFlutScreenProtocol1>();
        services.AddSingleton<GamePadsController>();
        services.AddSingleton<PixelBufferFactory>();
        services.AddSingleton<GameLoop>();
        services.AddLogging(logging => logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger()));
        services.AddTransient<PixelFlutScreen>();
        
        // Add games + Add Game configurations
        services.AddTransient<GameSelector>();
        services.AddTransient<GameBlackTestImage>();
        services.AddTransient<GameRainbowTestImage>();
        services.AddSingleton(Configuration.GetSection("RainbowTestImage").Get<GameRainbowTestImage.Configuration>());
        services.AddTransient<PongGame>();
        services.AddSingleton(Configuration.GetSection("Pong").Get<PongConfiguration>());


        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Create pixel game loop
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"- - - - - Starting pixelflut game - - - - - ");
        GamePadsController gamepadsController = serviceProvider.GetRequiredService<GamePadsController>();
        GameLoop gameLoop = serviceProvider.GetRequiredService<GameLoop>();

        // Setup gracefull shutdown
        CancellationTokenSource tokenSource = new();
        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            tokenSource.Cancel();
        };

        // Run
        Task t1 = Task.Run(async () => await gamepadsController.RunAsync(tokenSource.Token));
        Thread gameLoopThread = new(() => gameLoop.Run(tokenSource.Token));
        gameLoopThread.Start();
        gameLoopThread.Join();
        await Task.WhenAll(t1);

        logger.LogInformation($"- - - - -  Shutdown pixelflut game - - - - - ");
    }

    private static string RemoveSpaces(string s)
        => s.Replace(" ", "");

}


