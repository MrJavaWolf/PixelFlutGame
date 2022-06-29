using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PixelFlut.Core;
using System;
using System.Collections.Generic;

namespace PixelFlut.Pong.Tests
{
    [TestClass()]
    public class PongGameTests
    {
        PongConfiguration pongConfig = null!;
        PixelFlutScreenConfiguration screenConfiguration = null!;
        PongGameState pongGameState = null!;
        Mock<IGamePadDevice> gamePad1 = null!;
        Mock<IGamePadDevice> gamePad2 = null!;
        PongGame pongGame = null!;

        [TestInitialize]
        public void Init()
        {
            pongConfig = new()
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
            screenConfiguration = new()
            {
                ResultionX = 100,
                ResultionY = 100,
            };

            pongGameState = new()
            {
                BallPosition = new(
                    screenConfiguration.ResultionX / 2,
                    screenConfiguration.ResultionY / 2),
                Player1Position = new(
                    pongConfig.PlayerDistanceToSides,
                    screenConfiguration.ResultionY / 2 - pongConfig.PlayerHeight / 2),
                Player2Position = new(
                    screenConfiguration.ResultionX - pongConfig.PlayerDistanceToSides - pongConfig.PlayerWidth,
                    screenConfiguration.ResultionY / 2 - pongConfig.PlayerHeight / 2),
                CurrentGameState = PongGameStateType.Playing,
                BallVerlocity = new(-10, 0)
            };

            gamePad1 = new();
            gamePad2 = new();
            PixelFlutScreenProtocol1 screenProtocol = new PixelFlutScreenProtocol1();
            Mock<ILogger<PongGame>> logger = new();
            pongGame = new(
                pongConfig,
                screenConfiguration,
                new (screenProtocol, screenConfiguration),
                logger.Object,
                screenProtocol);
            pongGame.Startup(pongGameState);
        }

        [TestMethod()]
        public void LoopTest()
        {
            gamePad1.Setup(i => i.Y).Returns(0.5);
            gamePad2.Setup(i => i.Y).Returns(0);

            GameTime gameTime = new()
            {
                DeltaTime = TimeSpan.FromMilliseconds(1000),
                TotalTime = TimeSpan.FromMilliseconds(5000),
            };

            pongGameState.BallPosition = new(20, 50);
            pongGameState.Player1Position = new (pongGameState.Player1Position.X, 40);
            pongGameState.Player2Position = new (pongGameState.Player2Position.X, 40);
            pongGameState.BallVerlocity = new(-10, 0);
            pongGame.Loop(gameTime, new List<IGamePadDevice>() { gamePad1.Object, gamePad2.Object });

        }
    }
}