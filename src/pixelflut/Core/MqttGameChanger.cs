using MQTTnet;
using MQTTnet.Client;
using PixelFlut.Distributed;
using PixelFlut.Images;
using PixelFlut.Pong;
using PixelFlut.Snake;
using PixelFlut.TestImage;
using System.Text.Json;

namespace PixelFlut.Core;

public class MqttMessage
{
    public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;
    public string Source { get; set; } = Environment.MachineName;
    public string OS { get; set; } = Environment.OSVersion.VersionString;
    public PixelFlutScreenConfiguration? Screen { get; set; }
    public GameLoopConfiguration? GameLoop { get; set; }
    public DistributedServerConfiguration? DistributedServer { get; set; }
    public RainbowTestImage.Configuration? RainbowTestImage { get; set; }
    public GameImage.Configuration? Image { get; set; }
    public PongConfiguration? Pong { get; set; }
    public SnakeConfiguration? Snake { get; set; }
    public DistributedWorkerConfiguration? Distributed { get; set; }
    public MqttGameChangerConfiguration? Mqtt { get; set; }
}

public class MqttGameChangerConfiguration
{
    public bool Enable { get; set; } = false;
    public string MqttServer { get; set; } = "localhost";
    public string MqttTopic { get; set; } = "mqttnet/samples/topic/2";
    public string? PublishStatusMqttTopic { get; set; } = "mqttnet/samples/topic/3";
    public string? User { get; set; }
    public string? Password { get; set; }

}

public class MqttGameChanger : IDisposable
{
    private readonly PixelFlutScreenConfiguration screen;
    private readonly GameLoopConfiguration gameLoop;
    private readonly DistributedServerConfiguration distributedServer;
    private readonly RainbowTestImage.Configuration rainbowTestImage;
    private readonly GameImage.Configuration image;
    private readonly PongConfiguration pong;
    private readonly SnakeConfiguration snake;
    private readonly DistributedWorkerConfiguration distributed;


    private readonly MqttFactory mqttFactory;
    private readonly MqttGameChangerConfiguration config;
    private readonly ILogger<MqttGameChanger> logger;
    private readonly IMqttClient mqttClient;
    private MqttClientOptions? mqttClientOptions;
    private MqttClientSubscribeOptions? mqttClientSubscribeOptions;

    private MqttMessage? latestMqttMessage;

    public MqttGameChanger(
        PixelFlutScreenConfiguration screen,
        GameLoopConfiguration gameLoop,
        DistributedServerConfiguration distributedServer,
        RainbowTestImage.Configuration rainbowTestImage,
        GameImage.Configuration image,
        PongConfiguration pong,
        SnakeConfiguration snake,
        DistributedWorkerConfiguration distributed,
        MqttGameChangerConfiguration config,
        ILogger<MqttGameChanger> logger)
    {
        this.config = config;
        this.logger = logger;
        this.screen = screen;
        this.gameLoop = gameLoop;
        this.distributedServer = distributedServer;
        this.rainbowTestImage = rainbowTestImage;
        this.image = image;
        this.pong = pong;
        this.snake = snake;
        this.distributed = distributed;

        mqttFactory = new MqttFactory();
        mqttClient = mqttFactory.CreateMqttClient();
        if (!config.Enable) return;
        CreateMqttOptions();
    }

    private void CreateMqttOptions()
    {
        // Connection
        var mqttClientOptionsBuilder = new MqttClientOptionsBuilder();
        mqttClientOptionsBuilder.WithTcpServer(config.MqttServer);
        if (!string.IsNullOrWhiteSpace(config.User))
        {
            mqttClientOptionsBuilder.WithCredentials(config.User, config.Password);
        }
        mqttClientOptions = mqttClientOptionsBuilder.Build();

        // Topic
        mqttClientSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
          .WithTopicFilter(
              f =>
              {
                  f.WithTopic(config.MqttTopic);
              })
          .Build();
    }

    public MqttMessage? TryGetLatestMqttMessage()
    {
        MqttMessage? msg = latestMqttMessage;
        if (msg != null)
            latestMqttMessage = null;
        return msg;
    }

    public void Start()
    {
        if (!config.Enable) return;
        _ = Task.Run(async () => await StartAsync());
    }

    private async Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Connects to MQTT for updates: {@config}", config);
        mqttClient.ApplicationMessageReceivedAsync += OnRecivedMessageAsync;
        mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;

        await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
        await mqttClient.SubscribeAsync(mqttClientSubscribeOptions, cancellationToken);
        logger.LogInformation($"Successfully connected to the MQTT server {{@server}} with topic: {{@topic}}", mqttClientOptions, mqttClientSubscribeOptions);

        while (true)
        {
            if (!string.IsNullOrWhiteSpace(config.PublishStatusMqttTopic))
            {
                try
                {
                    MqttMessage statusMessage = new MqttMessage()
                    {
                        Distributed = this.distributed,
                        DistributedServer = this.distributedServer,
                        GameLoop = this.gameLoop,
                        Image = this.image,
                        Mqtt = this.config,
                        Pong = this.pong,
                        RainbowTestImage = this.rainbowTestImage,
                        Screen = this.screen,
                        Snake = this.snake
                    };
                    string jsonStatus = JsonSerializer.Serialize(statusMessage, new JsonSerializerOptions()
                    {
                        WriteIndented = true,
                    });
                    logger.LogInformation("Sends status to mqtt");
                    await mqttClient.PublishStringAsync(config.PublishStatusMqttTopic, jsonStatus);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to send MQTT status to topic: {config.PublishStatusMqttTopic}");
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(10, 20)));
        }
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        try
        {
            CreateMqttOptions();
            TimeSpan sleepTime = TimeSpan.FromSeconds(Random.Shared.Next(5, 10));
            logger.LogWarning($"Failed to connect to the MQTT server {{@server}} with topic: {{@topic}}, will retry in {sleepTime}...", mqttClientOptions, mqttClientSubscribeOptions);
            await Task.Delay(sleepTime);
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            await mqttClient.SubscribeAsync(mqttClientSubscribeOptions, CancellationToken.None);
            logger.LogInformation($"Successfully connected to the MQTT server {{@server}} with topic: {{@topic}}", mqttClientOptions, mqttClientSubscribeOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to reconnect");
        }
    }

    private Task OnRecivedMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            string payload = args.ApplicationMessage.ConvertPayloadToString();
            logger.LogInformation($"Received MQTT message: '{payload}'");
            MqttMessage? message = JsonSerializer.Deserialize<MqttMessage>(payload);

            if (message != null)
            {
                if (message.Screen == null &&
                    message.Distributed == null &&
                    message.DistributedServer == null &&
                    message.Snake == null &&
                    message.GameLoop == null &&
                    message.Image == null &&
                    message.Pong == null &&
                    message.Mqtt == null &&
                    message.RainbowTestImage == null)
                {
                    return Task.CompletedTask;
                }

                this.latestMqttMessage = message;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to handle MQTT message '{args.ApplicationMessage.ConvertPayloadToString()}'");
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        mqttClient.ApplicationMessageReceivedAsync -= OnRecivedMessageAsync;
        mqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
        MqttClientDisconnectOptionsBuilder builder = new();
        builder.WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        mqttClient.DisconnectAsync(builder.Build(), CancellationToken.None).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }
}
