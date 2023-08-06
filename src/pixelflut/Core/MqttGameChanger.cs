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
    public PixelFlutScreenConfiguration? Screen { get; set; }
    public GameLoopConfiguration? GameLoop { get; set; }
    public DistributedServerConfiguration? DistributedServer { get; set; }
    public RainbowTestImage.Configuration? RainbowTestImage { get; set; }
    public GameImage.Configuration? Image { get; set; }
    public PongConfiguration? Pong { get; set; }
    public SnakeConfiguration? Snake { get; set; }
    public DistributedWorkerConfiguration? Distributed { get; set; }

}

public class MqttGameChangerConfiguration
{
    public bool Enable { get; set; } = false;
    public string MqttServer { get; set; } = "localhost";
    public string MqttTopic { get; set; } = "mqttnet/samples/topic/2";
    public string? User { get; set; }
    public string? Password { get; set; }

}

public class MqttGameChanger
{
    private readonly MqttFactory mqttFactory;
    private readonly MqttGameChangerConfiguration config;
    private readonly ILogger<MqttGameChanger> logger;
    private readonly IMqttClient mqttClient;
    private MqttClientOptions? mqttClientOptions;
    private MqttClientSubscribeOptions? mqttClientSubscribeOptions;

    private MqttMessage? latestMqttMessage;

    public MqttGameChanger(
        MqttGameChangerConfiguration config,
        ILogger<MqttGameChanger> logger)
    {
        this.config = config;
        this.logger = logger;

        mqttFactory = new MqttFactory();
        mqttClient = mqttFactory.CreateMqttClient();
        if (!config.Enable) return;
        CreateMqttOptions(config);
    }

    private void CreateMqttOptions(MqttGameChangerConfiguration config)
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
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        TimeSpan sleepTime = TimeSpan.FromSeconds(Random.Shared.Next(5, 10));
        logger.LogWarning($"Failed to connect to the MQTT server, will retry in {sleepTime}...");
        await Task.Delay(sleepTime);
        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        await mqttClient.SubscribeAsync(mqttClientSubscribeOptions, CancellationToken.None);
    }

    private Task OnRecivedMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            logger.LogInformation($"Received MQTT message: {args.ApplicationMessage}");
            string payload = args.ApplicationMessage.ConvertPayloadToString();
            JsonSerializer.Deserialize<MqttMessage>(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle MQTT message");
        }
        return Task.CompletedTask;
    }
}
