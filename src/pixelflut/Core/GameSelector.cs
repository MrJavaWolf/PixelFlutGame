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
        }

        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            // TODO: Implement a game selection menu
            // TODO: Implement a way to exit one game and start another
            //       Could be triggered by a player pressing the 'Select' button
            return currentGame.Loop(time, gamePads);
        }
    }
}
