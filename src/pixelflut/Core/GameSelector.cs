namespace PixelFlut.Core
{
    public class GameSelector : IGame
    {
        private readonly GameLoopConfiguration config;
        private readonly GameFactory gameFactory;
        private readonly IServiceProvider serviceProvider;
        private IGame currentGame;

        public GameSelector(
            GameLoopConfiguration config,
            GameFactory gameFactory,
            IServiceProvider serviceProvider)
        {
            this.config = config;
            this.gameFactory = gameFactory;
            this.serviceProvider = serviceProvider;
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
