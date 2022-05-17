﻿using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PixelFlut.Core;
using System;

namespace PixelFlut.Pong.Tests
{
    [TestClass()]
    public class PongGameTests
    {
        PongConfiguration pongConfig = null!;
        PixelFlutScreenConfiguration screenConfiguration = null!;
        PongGameState pongGameState = null!;
        Mock<IGamePadInput> input = null!;
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

            input = new();
            PixelFlutScreenProtocol1 screenProtocol = new PixelFlutScreenProtocol1(screenConfiguration);
            Mock<ILogger<PongGame>> logger = new();
            pongGame = new(
                pongConfig,
                input.Object,
                screenConfiguration,
                new (screenProtocol, screenConfiguration),
                logger.Object,
                screenProtocol,
                pongGameState);
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

            pongGameState.BallPosition = new(20, 50);
            pongGameState.Player1Position = new (pongGameState.Player1Position.X, 40);
            pongGameState.Player2Position = new (pongGameState.Player2Position.X, 40);
            pongGameState.BallVerlocity = new(-10, 0);
            pongGame.Loop(gameTime);

        }
    }
}