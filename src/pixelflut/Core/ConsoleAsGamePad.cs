using System.Diagnostics;

namespace PixelFlut.Core
{
    /// <summary>
    /// Use the Consoles STD::IN as a gamepad
    /// This does not give a good experience but sometimes the console STD::IN is all you have
    /// Problems with the STD::IN
    /// - Only gives key down events like 30 times a second
    /// - Except for the first keypress then it waits about 500 ms before sending the next
    /// - No key up events, it just stops sending key down events
    /// - No support for multiple keys pressed at the same time, it will just send whatever key that was last pressed
    /// </summary>
    public class ConsoleAsGamePad : IGamePadDevice
    {
        private readonly ILogger<ConsoleAsGamePad> logger;
        private readonly StoppingToken stoppingToken;
        private bool isStartButtonCurrentlyPressedDown = false;
        private bool isSelectButtonCurrentlyPressedDown = false;
        private bool isNorthButtonCurrentlyPressedDown = false;
        private bool isEastButtonCurrentlyPressedDown = false;
        private bool isSouthButtonCurrentlyPressedDown = false;
        private bool isWestButtonCurrentlyPressedDown = false;

        // STD::IN only gives key down events ever so often like 30 times a second
        // STD::IN does NOT provide any no key up events so we simulate the key is pressed for at lest 250 ms
        // STD::IN only proveds the latest key that is pressed. If another key is pressed in the mean time the first key will be set as not pressed. 
        private TimeSpan userInputTime = TimeSpan.FromMilliseconds(250);
        private Stopwatch lastUserInput = new Stopwatch();
        
        public Task ConsoleListingTask;

        public double X { get; private set; } = 0.5;
        public double Y { get; private set; } = 0.5;

        public GamepadButton StartButton { get; private set; } = new GamepadButton();
        public GamepadButton SelectButton { get; private set; } = new GamepadButton();
        public GamepadButton NorthButton { get; private set; } = new GamepadButton();
        public GamepadButton EastButton { get; private set; } = new GamepadButton();
        public GamepadButton SouthButton { get; private set; } = new GamepadButton();
        public GamepadButton WestButton { get; private set; } = new GamepadButton();

        

        public ConsoleAsGamePad(ILogger<ConsoleAsGamePad> logger, StoppingToken stoppingToken)
        {
            this.logger = logger;
            this.stoppingToken = stoppingToken;
            this.ConsoleListingTask = Task.Run(RunAsync);
        }

        private async Task RunAsync()
        {
            while (!stoppingToken.Token.IsCancellationRequested)
            {
                await Task.Delay(10);
                if (!Console.KeyAvailable)
                {
                    if(lastUserInput.IsRunning && lastUserInput.Elapsed > userInputTime)
                    {
                        ResetAll();
                        lastUserInput.Stop();
                    }
                    continue;
                }
                var consoleInfo = Console.ReadKey();

                ResetAll();
                switch (consoleInfo.Key)
                {
                    case ConsoleKey.Enter:
                        isStartButtonCurrentlyPressedDown = true;
                        logger.LogInformation("Console input: Start Button");
                        break;
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        logger.LogInformation("Console input: Y Up");
                        Y = 0;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        logger.LogInformation("Console input: Y Down");
                        Y = 1;
                        break;
                    case ConsoleKey.Backspace:
                        logger.LogInformation("Console input: Select Button");
                        isSelectButtonCurrentlyPressedDown = true;
                        break;
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        logger.LogInformation("Console input: X Left");
                        X = 0;
                        break;
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        logger.LogInformation("Console input: X Right");
                        X = 1;
                        break;
                    case ConsoleKey.I:
                        logger.LogInformation("Console input: North Button");
                        isNorthButtonCurrentlyPressedDown = true;
                        break;
                    case ConsoleKey.J:
                        logger.LogInformation("Console input: East Button");
                        isEastButtonCurrentlyPressedDown = true;
                        break;
                    case ConsoleKey.K:
                        logger.LogInformation("Console input: South Button");
                        isSouthButtonCurrentlyPressedDown = true;
                        break;
                    case ConsoleKey.L:
                        logger.LogInformation("Console input: West Button");
                        isWestButtonCurrentlyPressedDown = true;
                        break;
                    default:
                        continue;
                }
                lastUserInput.Restart();
            }
        }

        public void ResetAll()
        {
            X = 0.5;
            Y = 0.5;
            isStartButtonCurrentlyPressedDown = false;
            isSelectButtonCurrentlyPressedDown = false;
            isNorthButtonCurrentlyPressedDown = false;
            isEastButtonCurrentlyPressedDown = false;
            isSouthButtonCurrentlyPressedDown = false;
            isWestButtonCurrentlyPressedDown = false;
        }

        public void Loop()
        {
            StartButton.Loop(isStartButtonCurrentlyPressedDown);
            SelectButton.Loop(isSelectButtonCurrentlyPressedDown);
            NorthButton.Loop(isNorthButtonCurrentlyPressedDown);
            EastButton.Loop(isEastButtonCurrentlyPressedDown);
            SouthButton.Loop(isSouthButtonCurrentlyPressedDown);
            WestButton.Loop(isWestButtonCurrentlyPressedDown);
        }
    }
}
