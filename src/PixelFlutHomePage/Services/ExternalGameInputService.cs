using PixelFlut.Core;
using System.Collections.Concurrent;

namespace PixelFlutHomePage.Services;

public class ExternalGameInputService
{
    private readonly PixelFlutServiceProvider pixelFlutServiceProvider;
    private readonly ILogger<ExternalGameInputService> logger;
    private ConcurrentDictionary<string, ExternalGameControllerInput> connectedGameControllers = new();

    public IReadOnlyCollection<(string, ExternalGameControllerInput)> ConnectedGameControllers =>
    connectedGameControllers.Select(x => (x.Key, x.Value)).ToList();

    public ExternalGameInputService(
        PixelFlutServiceProvider pixelFlutServiceProvider,
        ILogger<ExternalGameInputService> logger)
    {
        this.pixelFlutServiceProvider = pixelFlutServiceProvider;
        this.logger = logger;
        var _ = Task.Run(RunAutoCleanupAsync);
    }


    public void UpdateGameInput(
        string controllerId,
        ExternalGameControllerInputDto controllerState)
    {

        if (!connectedGameControllers.ContainsKey(controllerId))
        {
            var newExternalGameControllerInput = new ExternalGameControllerInput();
            connectedGameControllers.TryAdd(controllerId, newExternalGameControllerInput);
            pixelFlutServiceProvider.ServiceProvider
                .GetRequiredService<GamePadsController>()
                .AddExternalGamePadDevice(newExternalGameControllerInput);
            logger.LogInformation($"New external controller connected: {controllerId}");
        }

        if (connectedGameControllers.TryGetValue(controllerId, out var externalGameController))
        {
            externalGameController.X = controllerState.X;
            externalGameController.Y = controllerState.Y;
            externalGameController.StartButton.Loop(controllerState.IsStartButtonPressed);
            externalGameController.SelectButton.Loop(controllerState.IsSelectButtonPressed);
            externalGameController.NorthButton.Loop(controllerState.IsNorthButtonPressed);
            externalGameController.SouthButton.Loop(controllerState.IsSouthButtonPressed);
            externalGameController.EastButton.Loop(controllerState.IsEastButtonPressed);
            externalGameController.WestButton.Loop(controllerState.IsWestButtonPressed);
        }
    }

    private async Task RunAutoCleanupAsync()
    {
        while (true)
        {
            try
            {
                foreach (var connectedGameController in ConnectedGameControllers)
                {
                    if (DateTimeOffset.UtcNow - connectedGameController.Item2.LastUpdated > TimeSpan.FromSeconds(10))
                    {
                        connectedGameControllers.TryRemove(connectedGameController.Item1, out _);
                        logger.LogInformation($"External controller disconnected: {connectedGameController.Item1}");
                    }
                }
                await Task.Delay(500);
            }
            catch (Exception e)
            {
                logger.LogInformation(e, $"Auto cleanup error");
            }
        }
    }
}

public class ExternalGameControllerInputDto
{
    /// <summary>
    /// The left joystick X value
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The left joystick Y value
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// True if the start butten pressed down, otherwise false
    /// </summary>
    public bool IsStartButtonPressed { get; set; }

    /// <summary>
    /// True if the select butten pressed down, otherwise false
    /// </summary>
    public bool IsSelectButtonPressed { get; set; }

    /// <summary>
    /// True if the north butten pressed down, otherwise false
    /// </summary>
    public bool IsNorthButtonPressed { get; set; }

    /// <summary>
    /// True if the south butten pressed down, otherwise false
    /// </summary>
    public bool IsSouthButtonPressed { get; set; }

    /// <summary>
    /// True if the east butten pressed down, otherwise false
    /// </summary>
    public bool IsEastButtonPressed { get; set; }

    /// <summary>
    /// True if the west butten pressed down, otherwise false
    /// </summary>
    public bool IsWestButtonPressed { get; set; }

    public static ExternalGameControllerInputDto From(ExternalGameControllerInput externalGameControllerInput)
    {
        ExternalGameControllerInputDto dto = new ExternalGameControllerInputDto();
        dto.IsStartButtonPressed = externalGameControllerInput.StartButton.IsPressed;
        dto.IsSelectButtonPressed = externalGameControllerInput.SelectButton.IsPressed;
        dto.IsNorthButtonPressed = externalGameControllerInput.NorthButton.IsPressed;
        dto.IsSouthButtonPressed = externalGameControllerInput.SouthButton.IsPressed;
        dto.IsWestButtonPressed = externalGameControllerInput.WestButton.IsPressed;
        dto.IsEastButtonPressed = externalGameControllerInput.EastButton.IsPressed;
        dto.X = externalGameControllerInput.X;
        dto.Y = externalGameControllerInput.Y;
        return dto;
    }

}
public class ExternalGameControllerInput : IGamePadDevice
{
    public double X { get; set; } = 0.5;

    public double Y { get; set; } = 0.5;

    public GamepadButton StartButton { get; set; } = new GamepadButton();

    public GamepadButton SelectButton { get; set; } = new GamepadButton();

    public GamepadButton NorthButton { get; set; } = new GamepadButton();

    public GamepadButton EastButton { get; set; } = new GamepadButton();

    public GamepadButton SouthButton { get; set; } = new GamepadButton();

    public GamepadButton WestButton { get; set; } = new GamepadButton();

    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}