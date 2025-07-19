using GTweens.Extensions;
using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Numerics;

namespace pixelflut.LiveStreamReactions;

internal class LiveStreamReactionsGame : IGame
{
    private readonly ILogger<LiveStreamReactionsGame> logger;
    private readonly PixelBufferFactory bufferFactory;
    private readonly LiveStreamTelegramBot telegram;
    private readonly LiveStreamReactionsConfiguration config;
    public List<Image<Rgba32>> sprites = [];

    public List<Reaction> AutoActiveReactions = [];
    public List<Reaction> UserActiveReactions = [];

    private TimeSpan nextAllowedAutoSpawnTime = TimeSpan.Zero;
    public ConcurrentQueue<Image<Rgba32>> newReactions = [];


    public LiveStreamReactionsGame(
        ILogger<LiveStreamReactionsGame> logger,
        PixelBufferFactory bufferFactory,
        LiveStreamTelegramBot telegram,
        LiveStreamReactionsConfiguration config)
    {
        this.logger = logger;
        this.bufferFactory = bufferFactory;
        this.telegram = telegram;
        this.config = config;
        sprites.AddRange(LoadSprites(config, logger));
        Task telegramTask = Task.Run(telegram.StartAsync);
        telegram.OnStickerMessage += Telegram_OnMessage;
        telegram.OnTextMessage += Telegram_OnTextMessage;
    }


    private static List<Image<Rgba32>> LoadSprites(LiveStreamReactionsConfiguration config, ILogger logger)
    {
        List<Image<Rgba32>> loadedSprites = [];

        logger.LogInformation($"Loads image: {config.SpriteSheetFile}");

        byte[] spriteSheetBytes;
        if (File.Exists(config.SpriteSheetFile))
        {
            spriteSheetBytes = File.ReadAllBytes(config.SpriteSheetFile);
        }
        else if (File.Exists(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), config.SpriteSheetFile)))
        {
            spriteSheetBytes = File.ReadAllBytes(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), config.SpriteSheetFile));
        }
        else
        {
            throw new FileNotFoundException("Could not find file to display", config.SpriteSheetFile);
        }

        using Image<Rgba32> spriteSheet = Image.Load<Rgba32>(spriteSheetBytes);
        logger.LogInformation($"Image size: {spriteSheet.Width}, {spriteSheet.Height}");
        logger.LogInformation($"Sprite size: {config.SpriteWidth}, {config.SpriteHeight}");
        int columns = spriteSheet.Width / config.SpriteWidth;
        int rows = spriteSheet.Height / config.SpriteHeight;
        logger.LogInformation($"Columns in sprite sheet: {columns}");
        logger.LogInformation($"Rows in sprite sheet: {rows}");
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                var rect = new Rectangle(
                    x * config.SpriteWidth,
                    y * config.SpriteHeight,
                    config.SpriteWidth,
                    config.SpriteHeight);

                Image<Rgba32> sprite = spriteSheet.Clone();
                sprite.Mutate(ctx => ctx.Crop(rect));
                if (CountNumberOfVisiblePixels(sprite) == 0)
                {
                    logger.LogInformation($"Skips sprite {x},{y} - Does not contain any visible pixels");
                    continue;
                }
                loadedSprites.Add(sprite);
            }
        }
        logger.LogInformation($"Number of loaded sprites in sprite sheet: {loadedSprites.Count}");
        return loadedSprites;
    }

    private static int CountNumberOfVisiblePixels(Image<Rgba32> sprite)
    {
        int visiblePixels = 0;
        for (int y = 0; y < sprite.Height; y++)
        {
            for (int x = 0; x < sprite.Width; x++)
            {
                var pixel = sprite[x, y];
                if (pixel.A > 0)
                {
                    visiblePixels++;
                }
            }
        }
        return visiblePixels;
    }

    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        // Check if a player have requested a reaction
        if (gamePads.Any(x => x.StartButton.OnPress))
        {
            logger.LogInformation("Player have made an reaction");
            var reaction = CreateRandomReaction(time);
            if (reaction != null)
            {
                UserActiveReactions.Add(reaction);
            }
            else
            {
                logger.LogInformation("Failed to create reactions, no sprites loaded from spritesheet");
            }
        }

        // reactions from message apps
        while (newReactions.TryDequeue(out var reactionSprite))
        {
            var reaction = CreateReaction(time, reactionSprite);
            UserActiveReactions.Add(reaction);
        }

        // Auto spawn reactions
        if (UserActiveReactions.Count + AutoActiveReactions.Count < config.AutoSpawnKeepAliveAmount &&
            nextAllowedAutoSpawnTime < time.TotalTime)
        {
            //logger.LogInformation("Auto spawns a reaction");
            var reaction = CreateRandomReaction(time);
            if (reaction != null)
            {
                AutoActiveReactions.Add(reaction);
            }
            else
            {
                logger.LogInformation("Failed to create reactions, no sprites loaded from spritesheet");
            }

            nextAllowedAutoSpawnTime = time.TotalTime +
                TimeSpan.FromMilliseconds(
                    config.MinTimeBetweenAutoSpawnReactions.TotalMilliseconds +
                    Random.Shared.NextSingle() * (config.MaxTimeBetweenAutoSpawnReactions.TotalMilliseconds - config.MinTimeBetweenAutoSpawnReactions.TotalMilliseconds));
        }

        UpdateReactions(time, AutoActiveReactions);
        UpdateReactions(time, UserActiveReactions);
        telegram.UpdateNumberOfActiveReactions(UserActiveReactions.Count);
        var pixelBuffers = AutoActiveReactions.Select(x => x.PixelBuffer).ToList();
        pixelBuffers.AddRange(UserActiveReactions.Select(x => x.PixelBuffer).ToList());
        return pixelBuffers;
    }

    private void UpdateReactions(GameTime time, List<Reaction> reactions)
    {
        // Update reactions
        ConcurrentBag<Reaction> completedReactions = [];
        Parallel.For(0, reactions.Count, (i) =>
        {
            reactions[i].Tween?.Tick((float)time.DeltaTime.TotalSeconds);
            if (reactions[i].Tween?.IsCompletedOrKilled == true)
            {
                completedReactions.Add(reactions[i]);
            }
        });

        foreach (var reaction in completedReactions)
        {
            reactions.Remove(reaction);
        }
    }

    private Reaction? CreateRandomReaction(GameTime time)
    {
        if (sprites.Count <= 0)
        {
            return null;
        }

        Image<Rgba32> reactionSprite = sprites[Random.Shared.Next(sprites.Count)];
        return CreateReaction(time, reactionSprite);
    }

    private Reaction CreateReaction(GameTime time, Image<Rgba32> sprite)
    {
        int numberOfVisiblePixels = CountNumberOfVisiblePixels(sprite);
        int startX = Random.Shared.Next(config.SpawnStartX, config.SpawnEndX);
        int startY = Random.Shared.Next(config.SpawnStartY, config.SpawnEndY);
        int endX = startX + Random.Shared.Next(config.MinXMovement, config.MaxXMovement);
        int endY = startY + Random.Shared.Next(config.MinYMovement, config.MaxYMovement);
        TimeSpan duration = TimeSpan.FromMilliseconds(
                    config.MinimumLifeTime.TotalMilliseconds +
                    Random.Shared.NextSingle() * (config.MaximumLifeTime.TotalMilliseconds - config.MinimumLifeTime.TotalMilliseconds));

        PixelBuffer pixelBuffer = bufferFactory.Create(numberOfVisiblePixels);
        int pixelNumber = 0;
        for (int y = 0; y < sprite.Height; y++)
        {
            for (int x = 0; x < sprite.Width; x++)
            {
                var pixel = sprite[x, y];
                if (pixel.A == 0) continue;
                pixelBuffer.SetPixel(
                    pixelNumber,
                    startX + x,
                    startY + y,
                    pixel.R,
                    pixel.G,
                    pixel.B,
                    pixel.A);
                pixelNumber++;
            }
        }
        Vector2 startPosition = new Vector2(startX, startY);
        Vector2 endPosition = new Vector2(endX, endY);
        Reaction reaction = new Reaction()
        {
            Duration = duration,
            StartPosition = startPosition,
            CurrentPosition = startPosition,
            EndPosition = endPosition,
            StartTime = time.TotalTime,
            PixelBuffer = pixelBuffer,
            Sprite = sprite
        };
        reaction.Tween = GTweenExtensions.Tween(
            () => reaction.CurrentPosition,
            newPosition => reaction.UpdateLocation(reaction, newPosition),
            endPosition,
            (float)duration.TotalSeconds)
            .SetEasing(GTweens.Easings.Easing.OutBounce);
        reaction.Tween.Start();
        return reaction;

    }

    private void Telegram_OnMessage(object? sender, Image<Rgba32> e)
    {
        newReactions.Enqueue(e);
    }

    private void Telegram_OnTextMessage(object? sender, Image<Rgba32> e)
    {
        newReactions.Enqueue(e);
    }
}
