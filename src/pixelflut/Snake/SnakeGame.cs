using PixelFlut.Core;

namespace PixelFlut.Snake;


public class SnakeGame : IGame
{
    private readonly SnakeConfiguration snakeConfiguration;
    private readonly PixelBufferFactory bufferFactory;
    private readonly ILogger<SnakeGame> logger;
    private SnakeState snakeState = new SnakeState();
    private SnakeRendererBuffers rendererBuffers;
    public SnakeGame(
        SnakeConfiguration snakeConfiguration,
        PixelBufferFactory bufferFactory,
        ILogger<SnakeGame> logger)
    {
        this.snakeConfiguration = snakeConfiguration;
        this.bufferFactory = bufferFactory;
        this.logger = logger;
        logger.LogInformation("Snake game configuration: {@snakeConfiguration}", snakeConfiguration);
        Reset();
        logger.LogInformation("Preparing snake game renderer buffers");
        rendererBuffers = SnakeRenderer.PrepareBuffers(bufferFactory, snakeConfiguration, snakeState);
        logger.LogInformation("Snake game renderer buffers prepared");
    }

    public void Reset()
    {
        snakeState = new SnakeState
        {
            Direction = SnakeState.Directions.Down,
            AreaSize = (
                bufferFactory.Screen.ResolutionX / snakeConfiguration.TileWidth,
                bufferFactory.Screen.ResolutionY / snakeConfiguration.TileHeight),
            TimeBetweenSteps = snakeConfiguration.StartTimeBetweenSteps
        };

        int startX = snakeState.AreaSize.Width / 2;
        int startY = snakeState.AreaSize.Height / 2;
        for (int i = 0; i < snakeConfiguration.SnakeStartSize; i++)
        {
            snakeState.Snake.Add((startX, startY));
            startY -= 1;
        }
        SpawnFood();

        logger.LogInformation("Snake game reset");
    }

    public void SpawnFood()
    {
        snakeState.Food = (
            Random.Shared.Next(snakeState.AreaSize.Width),
            Random.Shared.Next(snakeState.AreaSize.Height));
    }

    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        if (gamePads.Any(g => g.X > 0.8) && snakeState.Direction != SnakeState.Directions.Left)
            snakeState.Direction = SnakeState.Directions.Right;
        else if (gamePads.Any(g => g.X < 0.2) && snakeState.Direction != SnakeState.Directions.Right)
            snakeState.Direction = SnakeState.Directions.Left;
        else if (gamePads.Any(g => g.Y > 0.8) && snakeState.Direction != SnakeState.Directions.Up)
            snakeState.Direction = SnakeState.Directions.Down;
        else if (gamePads.Any(g => g.Y < 0.2) && snakeState.Direction != SnakeState.Directions.Down)
            snakeState.Direction = SnakeState.Directions.Up;

        if (time.TotalTime - snakeState.LastMoveTime > snakeState.TimeBetweenSteps)
        {
            MovePlayer();
            snakeState.LastMoveTime = time.TotalTime;
            logger.LogInformation("Snake moves");
        }

        return SnakeRenderer.Renderer(rendererBuffers, snakeState);
    }


    private void MovePlayer()
    {
        // the main loop for the snake head and parts
        for (int i = snakeState.Snake.Count - 1; i >= 0; i--)
        {
            // if the snake head is active 
            if (i == 0)
            {

                // move rest of the body according to which way the head is moving
                switch (snakeState.Direction)
                {
                    case SnakeState.Directions.Right:
                        snakeState.Snake[i] = new(
                            snakeState.Snake[i].X + 1,
                            snakeState.Snake[i].Y);
                        break;
                    case SnakeState.Directions.Left:
                        snakeState.Snake[i] = new(
                            snakeState.Snake[i].X - 1,
                            snakeState.Snake[i].Y);
                        break;
                    case SnakeState.Directions.Up:
                        snakeState.Snake[i] = new(
                            snakeState.Snake[i].X,
                            snakeState.Snake[i].Y - 1);
                        break;
                    case SnakeState.Directions.Down:
                        snakeState.Snake[i] = new(
                            snakeState.Snake[i].X,
                            snakeState.Snake[i].Y + 1);
                        break;
                }

                if (snakeState.Snake[i].X < 0 ||
                    snakeState.Snake[i].Y < 0 ||
                    snakeState.Snake[i].X >= snakeState.AreaSize.Width ||
                    snakeState.Snake[i].Y >= snakeState.AreaSize.Height)
                {
                    // end the game is snake either reaches edge of the canvas
                    Die();
                }

                // detect collision with the body
                // this loop will check if the snake had an collision with other body parts
                for (int j = 1; j < snakeState.Snake.Count; j++)
                {
                    if (snakeState.Snake[i].X == snakeState.Snake[j].X && snakeState.Snake[i].Y == snakeState.Snake[j].Y)
                    {
                        // if so we run the die function
                        Die();
                    }
                }

                // detect collision between snake head and food
                if (snakeState.Snake[0].X == snakeState.Food.X && snakeState.Snake[0].Y == snakeState.Food.Y)
                {
                    //if so we run the eat function
                    Eat();
                }
            }
            else
            {
                // if there are no collisions then we continue moving the snake and its parts
                snakeState.Snake[i] = (
                    snakeState.Snake[i - 1].X,
                    snakeState.Snake[i - 1].Y);
            }
        }
    }

    private void Eat()
    {
        logger.LogInformation("Snake ate food");
        // Add a part to body
        snakeState.Snake.Add((
            snakeState.Snake[snakeState.Snake.Count - 1].X,
            snakeState.Snake[snakeState.Snake.Count - 1].Y));

        // Increase speed
        snakeState.TimeBetweenSteps = snakeState.TimeBetweenSteps * snakeConfiguration.TimeBetweenStepsDecreasePerFood;

        // More food
        SpawnFood();
    }

    private void Die()
    {

        logger.LogInformation("Snake died");
        Reset();
    }
}
