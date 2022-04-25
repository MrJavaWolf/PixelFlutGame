using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixelFlut.PingPong;
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
        PixelFlutRendererConfiguration rendererConfig = Configuration.GetSection("Renderer").Get<PixelFlutRendererConfiguration>();
        PixelFlutGamepadConfiguration gamepadConfig = Configuration.GetSection("Gamepad").Get<PixelFlutGamepadConfiguration>();
        GameLoopConfiguration gameloopConfig = Configuration.GetSection("GameLoop").Get<GameLoopConfiguration>();
        PingPongConfiguration pingPongConfig = Configuration.GetSection("PingPong").Get<PingPongConfiguration>();

        // Dependency injection
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddSingleton(rendererConfig);
        services.AddSingleton(gamepadConfig);
        services.AddSingleton(gameloopConfig);
        services.AddSingleton(pingPongConfig);
        services.AddSingleton<PixelFlutGamepad>();
        services.AddSingleton<PingPongGame>();
        services.AddSingleton<GameLoop>();
        services.AddLogging(logging => logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger()));
        services.AddTransient<PixelFlutRenderer>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Create pixel game loop
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"- - - - - Starting pixelflut game - - - - - ");
        PixelFlutGamepad gamepad = serviceProvider.GetRequiredService<PixelFlutGamepad>();
        GameLoop gameLoop = serviceProvider.GetRequiredService<GameLoop>();

        // Setup gracefull shutdown
        CancellationTokenSource tokenSource = new();
        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            tokenSource.Cancel();
        };

        // Run
        Task t1 = Task.Run(async () => await gamepad.RunAsync(tokenSource.Token));
        Task t2 = Task.Run(async () => await gameLoop.RunAsync(tokenSource.Token));
        await Task.WhenAll(t1, t2);

        logger.LogInformation($"- - - - -  Shutdown pixelflut game - - - - - ");
    }


}
       

