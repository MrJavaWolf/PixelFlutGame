using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pixelflut;
using Serilog;

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
services.AddSingleton<PixelflutPingPong>();
services.AddSingleton<GameLoop>();
services.AddLogging(logging =>logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger()));
services.AddTransient<PixelFlutRenderer>();
ServiceProvider serviceProvider = services.BuildServiceProvider();

// Run
ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation($"- - - - - Starting pixelflut game - - - - - ");
PixelFlutGamepad gamepad = serviceProvider.GetRequiredService<PixelFlutGamepad>();
GameLoop gameLoop = serviceProvider.GetRequiredService<GameLoop>();
Task.Run(async () => await gamepad.RunAsync(CancellationToken.None));
await Task.Run(async () => await gameLoop.RunAsync(CancellationToken.None));
logger.LogInformation($"- - - - -  Shutdown pixelflut game - - - - - ");

