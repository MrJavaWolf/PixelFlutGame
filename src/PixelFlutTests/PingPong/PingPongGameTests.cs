using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PixelFlut.Core;
using System;

namespace PixelFlut.PingPong.Tests
{
    [TestClass()]
    public class PingPongGameTests
    {
        PingPongConfiguration pingPongConfig = null!;
        PixelFlutScreenRendererConfiguration pixelFlutRendererConfiguration = null!;
        PingPongGameState pingPongGameState = null!;
        Mock<IPixelFlutInput> input = null!;
        PingPongGame pingPongGame = null!;

        [TestInitialize]
        public void Init()
        {
            pingPongConfig = new()
            {
                BallBorder = 3,
                BallRadius = 3,
                BallSpeedIncrease = 5,
                BallStartSpeed = 10,
                NumberOfGoalsToWin = 1,
                PlayerBorder = 3,
                PlayerDistanceToSides = 10,
                PlayerHeight = 20,
                PlayerSpeed = 5,
                PlayerWidth = 4,
            };
            pixelFlutRendererConfiguration = new()
            {
                ResultionX = 100,
                ResultionY = 100,
            };

            pingPongGameState = new()
            {
                BallPosition = new(
                    pixelFlutRendererConfiguration.ResultionX / 2,
                    pixelFlutRendererConfiguration.ResultionY / 2),
                Player1Position = new(
                    pingPongConfig.PlayerDistanceToSides,
                    pixelFlutRendererConfiguration.ResultionY / 2 - pingPongConfig.PlayerHeight / 2),
                Player2Position = new(
                    pixelFlutRendererConfiguration.ResultionX - pingPongConfig.PlayerDistanceToSides - pingPongConfig.PlayerWidth,
                    pixelFlutRendererConfiguration.ResultionY / 2 - pingPongConfig.PlayerHeight / 2),
                CurrentGameState = PingPongGameStateType.Playing,
                BallVerlocity = new(-10, 0)
            };

            input = new();
            Mock<ILogger<PingPongGame>> logger = new();
            pingPongGame = new(
                pingPongConfig,
                input.Object,
                pixelFlutRendererConfiguration,
                logger.Object,
                pingPongGameState);
        }

        [TestMethod()]
        public void LoopTest()
        {
            input.Setup(i => i.Y).Returns(0.5);
            GameTime gameTime = new()
            {
                DeltaTime = TimeSpan.FromMilliseconds(1000),
                TotalTime = TimeSpan.FromMilliseconds(5000),
            };

            pingPongGameState.BallPosition = new(20, 50);
            pingPongGameState.Player1Position = new (pingPongGameState.Player1Position.X, 40);
            pingPongGameState.Player2Position = new (pingPongGameState.Player2Position.X, 40);
            pingPongGameState.BallVerlocity = new(-10, 0);
            pingPongGame.Loop(gameTime);

        }
    }
}