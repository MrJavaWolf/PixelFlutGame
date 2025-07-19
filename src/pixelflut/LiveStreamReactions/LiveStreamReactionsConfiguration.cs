using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pixelflut.LiveStreamReactions;

public class LiveStreamReactionsConfiguration
{
    public required string SpriteSheetFile { get; set; }

    public required int SpriteHeight { get; set; } = 32;

    public required int SpriteWidth { get; set; } = 32;

    public required int SpawnStartX { get; set; }
    public required int SpawnEndX { get; set; }

    public required int MinXMovement { get; set; }
    public required int MaxXMovement { get; set; }

    public required int SpawnStartY { get; set; }
    public required int SpawnEndY { get; set; }
    public required int MinYMovement { get; set; }
    public required int MaxYMovement { get; set; }
    public required TimeSpan MinimumLifeTime { get; set; }
    public required TimeSpan MaximumLifeTime { get; set; }

    public required int AutoSpawnKeepAliveAmount { get; set; }
    public required TimeSpan MinTimeBetweenAutoSpawnReactions { get; set; }
    public required TimeSpan MaxTimeBetweenAutoSpawnReactions { get; set; }
    
    public required LiveStreamTelegramBotConfiguration Telegram { get; set; }
}

