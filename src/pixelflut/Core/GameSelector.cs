using PixelFlut.Distributed;
using PixelFlut.Images;
using PixelFlut.Pong;
using PixelFlut.Snake;
using PixelFlut.TestImage;

namespace PixelFlut.Core
{
    public class GameSelector : IGame
    {
        private readonly GameLoopConfiguration config;
        private readonly GameFactory gameFactory;
        private readonly MqttGameChanger mqttGameChanger;
        private readonly IServiceProvider serviceProvider;
        private readonly PixelFlutScreenConfiguration screen;
        private readonly GameLoopConfiguration gameLoop;
        private readonly DistributedServerConfiguration distributedServer;
        private readonly RainbowTestImage.Configuration rainbowTestImage;
        private readonly GameImage.Configuration image;
        private readonly PongConfiguration pong;
        private readonly SnakeConfiguration snake;
        private readonly DistributedWorkerConfiguration distributed;
        private readonly ILogger<GameSelector> logger;
        private IGame currentGame;

        public GameSelector(
            GameLoopConfiguration config,
            GameFactory gameFactory,
            MqttGameChanger mqttGameChanger,
            IServiceProvider serviceProvider,
            PixelFlutScreenConfiguration screen,
            GameLoopConfiguration gameLoop,
            DistributedServerConfiguration distributedServer,
            RainbowTestImage.Configuration rainbowTestImage,
            GameImage.Configuration image,
            PongConfiguration pong,
            SnakeConfiguration snake,
            DistributedWorkerConfiguration distributed,
            ILogger<GameSelector> logger)
        {
            this.config = config;
            this.gameFactory = gameFactory;
            this.mqttGameChanger = mqttGameChanger;
            this.serviceProvider = serviceProvider;
            this.screen = screen;
            this.gameLoop = gameLoop;
            this.distributedServer = distributedServer;
            this.rainbowTestImage = rainbowTestImage;
            this.image = image;
            this.pong = pong;
            this.snake = snake;
            this.distributed = distributed;
            this.logger = logger;
            logger.LogInformation($"Creates game: {this.config.GameToPlay}");
            currentGame = this.gameFactory.CreateGame(this.config.GameToPlay, this.serviceProvider);
            mqttGameChanger.Start();
        }

        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            MqttMessage? msg = mqttGameChanger.TryGetLatestMqttMessage();
            if (msg != null)
            {
                UpdateConfigurations(msg);
                logger.LogInformation($"Creates game: {this.config.GameToPlay}");
                currentGame = this.gameFactory.CreateGame(this.config.GameToPlay, this.serviceProvider);
            }
            // TODO: Implement a game selection menu
            // TODO: Implement a way to exit one game and start another
            //       Could be triggered by a player pressing the 'Select' button
            return currentGame.Loop(time, gamePads);
        }

        private void UpdateConfigurations(MqttMessage msg)
        {
            if (msg.Image != null)
            {
                this.image.Image = msg.Image.Image;
                this.image.Speed = msg.Image.Speed;
                this.image.AutoMove.SpeedX = msg.Image.AutoMove.SpeedX;
                this.image.AutoMove.SpeedY = msg.Image.AutoMove.SpeedY;
                this.image.AutoMove.Enable = msg.Image.AutoMove.Enable;
                this.image.AutoMove.MaxX = msg.Image.AutoMove.MaxX;
                this.image.AutoMove.MaxY = msg.Image.AutoMove.MaxY;
                this.image.AutoMove.MinY = msg.Image.AutoMove.MinY;
                this.image.AutoMove.MinX = msg.Image.AutoMove.MinX;
            }
            if (msg.Snake != null)
            {
                this.snake.SnakeStartSize = msg.Snake.SnakeStartSize;
                this.snake.TileBorderSize = msg.Snake.TileBorderSize;
                this.snake.TileWidth = msg.Snake.TileWidth;
                this.snake.TileHeight = msg.Snake.TileHeight;
                this.snake.TimeBetweenStepsDecreasePerFood = msg.Snake.TimeBetweenStepsDecreasePerFood;
                this.snake.StartTimeBetweenSteps = msg.Snake.StartTimeBetweenSteps;
            }
            if (msg.RainbowTestImage != null)
            {
                this.rainbowTestImage.TestImageOffset = msg.RainbowTestImage.TestImageOffset;
                this.rainbowTestImage.Moving = msg.RainbowTestImage.Moving;
            }
            if (msg.Screen != null)
            {
                this.screen.Ip = msg.Screen.Ip;
                this.screen.Port = msg.Screen.Port;
                this.screen.OffsetX = msg.Screen.OffsetX;
                this.screen.OffsetY = msg.Screen.OffsetY;
                this.screen.ResolutionX = msg.Screen.ResolutionX;
                this.screen.ResolutionY = msg.Screen.ResolutionY;
                this.screen.SleepTimeBetweenSends = msg.Screen.SleepTimeBetweenSends;
                this.screen.SenderThreads = msg.Screen.SenderThreads;
            }
            if (msg.Distributed != null)
            {
                this.distributed.Port = msg.Distributed.Port;
                this.distributed.Ip = msg.Distributed.Ip;
            }
            if (msg.DistributedServer != null)
            {
                this.distributedServer.Port = msg.DistributedServer.Port;
                this.distributedServer.NumberOfBuffersPerFrame = msg.DistributedServer.NumberOfBuffersPerFrame;
                this.distributedServer.Enable = msg.DistributedServer.Enable;
            }
            if (msg.GameLoop != null)
            {
                this.gameLoop.TargetGameLoopFPS = msg.GameLoop.TargetGameLoopFPS;
                this.gameLoop.GameToPlay = msg.GameLoop.GameToPlay;
            }
            if (msg.Pong != null)
            {
                this.pong.BallBorder = msg.Pong.BallBorder;
                this.pong.BallRadius = msg.Pong.BallRadius;
                this.pong.PlayerBorder = msg.Pong.PlayerBorder;
                this.pong.NumberOfGoalsToWin = msg.Pong.NumberOfGoalsToWin;
                this.pong.BallSpeedIncrease = msg.Pong.BallSpeedIncrease;
                this.pong.PlayerDistanceToSides = msg.Pong.PlayerDistanceToSides;
                this.pong.PlayerHeight = msg.Pong.PlayerHeight;
                this.pong.PlayerWidth = msg.Pong.PlayerWidth;
                this.pong.PlayerSpeed = msg.Pong.PlayerSpeed;
                this.pong.BallStartSpeed = msg.Pong.BallStartSpeed;
                this.pong.PlayerBorder = msg.Pong.PlayerBorder;
                this.pong.PlayerMaxRebounceAngle = msg.Pong.PlayerMaxRebounceAngle;
            }
        }
    }
}
