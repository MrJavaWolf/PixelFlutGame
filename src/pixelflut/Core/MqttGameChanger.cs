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
    public MqttGameChangerConfiguration? Mqtt { get; set; }
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
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        CreateMqttOptions();
        TimeSpan sleepTime = TimeSpan.FromSeconds(Random.Shared.Next(5, 10));
        logger.LogWarning($"Failed to connect to the MQTT server, will retry in {sleepTime}...");
        await Task.Delay(sleepTime);
        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        await mqttClient.SubscribeAsync(mqttClientSubscribeOptions, CancellationToken.None);
    }

    private async Task OnRecivedMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            logger.LogInformation($"Received MQTT message: {args.ApplicationMessage}");
            string payload = args.ApplicationMessage.ConvertPayloadToString();
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
                    return;
                }

                if (message.Mqtt != null)
                {
                    this.config.Enable = message.Mqtt.Enable;
                    this.config.MqttTopic = message.Mqtt.MqttTopic;
                    this.config.MqttServer = message.Mqtt.MqttServer;
                    this.config.Password = message.Mqtt.Password;
                    this.config.User = message.Mqtt.User;
                    MqttClientDisconnectOptionsBuilder builder = new();
                    builder.WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection);
                    await mqttClient.DisconnectAsync(builder.Build(), CancellationToken.None);
                }
                this.latestMqttMessage = message;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to handle MQTT message {args.ApplicationMessage}");
        }
    }
}
