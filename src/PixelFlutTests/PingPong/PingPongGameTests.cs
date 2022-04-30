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
        PixelFlutRendererConfiguration pixelFlutRendererConfiguration = null!;
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
                BallXPosition = pixelFlutRendererConfiguration.ResultionX / 2,
                BallYPosition = pixelFlutRendererConfiguration.ResultionY / 2,
                Player1PositionX = pingPongConfig.PlayerDistanceToSides,
                Player1PositionY = pixelFlutRendererConfiguration.ResultionY / 2 - pingPongConfig.PlayerHeight / 2,
                Player2PositionX = pixelFlutRendererConfiguration.ResultionX - pingPongConfig.PlayerDistanceToSides - pingPongConfig.PlayerWidth,
                Player2PositionY = pixelFlutRendererConfiguration.ResultionY / 2 - pingPongConfig.PlayerHeight / 2,
                CurrentGameState = PingPongGameStateType.Playing,
                BallXVerlocity = -10,
                BallYVerlocity = 0,
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

            pingPongGameState.BallXPosition = 20;
            pingPongGameState.BallYPosition = 50;
            pingPongGameState.Player1PositionY = 40;
            pingPongGameState.Player2PositionY = 40;
            pingPongGameState.BallXVerlocity = -10;
            pingPongGameState.BallYVerlocity = 0;
            pingPongGame.Loop(gameTime);
            
        }
    }
}