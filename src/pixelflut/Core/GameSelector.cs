using Microsoft.Extensions.DependencyInjection;
using PixelFlut.Images;
using PixelFlut.Pong;
using PixelFlut.TestImage;

namespace PixelFlut.Core
{
    public class GameSelector : IGame
    {
        private readonly ILogger<GameSelector> logger;
        private readonly IServiceProvider serviceProvider;
        private IGame currentGame;

        public GameSelector(
            ILogger<GameSelector> logger, 
            GameLoopConfiguration config,
            IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            currentGame = CreateGame(config.GameToPlay);
        }

        public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
        {
            return currentGame.Loop(time, gamePads);
        }

        private IGame CreateGame(string gameName)
        {
            logger.LogInformation($"Creates now game: '{gameName}'");
            IGame game = gameName switch
            {
                "Pong" => serviceProvider.GetRequiredService<PongGame>(),
                "RainbowTestImage" => serviceProvider.GetRequiredService<GameRainbowTestImage>(),
                "BlackTestImage" => serviceProvider.GetRequiredService<GameBlackTestImage>(),
                "Image" => serviceProvider.GetRequiredService<GameImage>(),
                _ => throw new NotSupportedException($"Unknown game name: '{gameName}'"),
            };
            return game;
        }
    }
}
