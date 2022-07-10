using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PixelFlut.Core
{
    public class GameFactory
    {
        private readonly IConfiguration configuration;
        private Dictionary<string, Type> games = new Dictionary<string, Type>();
        public GameFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IGame CreateGame(string gameName, IServiceProvider serviceProvider)
        {
            if (!games.ContainsKey(gameName))
                throw new NotSupportedException($"Unknown game name: '{gameName}'");

            IGame? game = serviceProvider.GetRequiredService(games[gameName]) as IGame;

            if (game == null)
                throw new NotSupportedException($"Failed to cast '{gameName}' to {nameof(IGame)}, does it not implement the {nameof(IGame)} interface?");

            return game;
        }

        /// <summary>
        /// Register the game
        /// </summary>
        /// <typeparam name="TGame">The game you want to register</typeparam>
        /// <typeparam name="TGameConfig">Game configuration</typeparam>
        /// <param name="gameName">The name of the game, this have to be a unique name</param>
        public void AddGame<TGame>(string gameName, ServiceCollection services)
            where TGame : class, IGame
        {
            services.AddTransient<TGame>();
            games.Add(gameName, typeof(TGame));
        }

        /// <summary>
        /// Register the game
        /// </summary>
        /// <typeparam name="TGame">The game you want to register</typeparam>
        /// <typeparam name="TGameConfig">Game configuration</typeparam>
        /// <param name="gameName">The name of the game, this have to be a unique name</param>
        public void AddGame<TGame, TGameConfig>(string gameName, ServiceCollection services)
            where TGame : class, IGame
            where TGameConfig : class
        {
            AddGame<TGame>(gameName, services);
            services.AddSingleton(configuration.GetSection(gameName).Get<TGameConfig>());
        }
    }
}
