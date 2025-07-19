using pixelflut.Core;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace pixelflut.LiveStreamReactions;


public class LiveStreamTelegramBotConfiguration
{
    public required bool Enable { get; set; }
    public required string BotToken { get; set; }
    public required int StickerXSize { get; set; }
    public required int StickerYSize { get; set; }
    public required string FontFile { get; set; }
    public required int FontSize { get; set; }
    
}

public class LiveStreamTelegramBot(
    LiveStreamReactionsConfiguration config,
    FileLoader fileLoader,
    ILogger<LiveStreamTelegramBot> logger)
{
    private TelegramBotClient? bot;
    private User? botUser;
    public event EventHandler<Image<Rgba32>>? OnMessage;
    private SixLabors.Fonts.Font? font;

    public async Task StartAsync()
    {
        if (!config.Telegram.Enable)
        {
            logger.LogInformation("LiveStreamTelegramBot is DISABLED");
        }
        FontCollection collection = new();
        FontFamily family = collection.Add(fileLoader.FullFileName(config.Telegram.FontFile));
        font = family.CreateFont(config.Telegram.FontSize, FontStyle.Regular);

        try
        {
            bot = new TelegramBotClient(config.Telegram.BotToken);
            botUser = await bot.GetMe();
            bot.OnMessage += Bot_OnMessageAsync;
            logger.LogInformation($"Hello, World! I am user {botUser.Id} and my name is {botUser.FirstName}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Telegram bot encountered an error and stopped working");
        }
    }

    private async Task Bot_OnMessageAsync(Telegram.Bot.Types.Message message, Telegram.Bot.Types.Enums.UpdateType type)
    {
        if (bot == null || botUser == null || font == null)
        {
            return;
        }

        try
        {
            if (message.Text != null)
            {
                logger.LogInformation($"Recieved a Text message: {message.Text}");

                string textToRender = WrapText(message.Text, 50);
                TextOptions options = new(font);
                FontRectangle rect = TextMeasurer.MeasureAdvance(message.Text, options);
                Image<Rgba32> textImage = new Image<Rgba32>((int)rect.Width, (int)rect.Height);
                textImage.Mutate(x => x.DrawText(message.Text, font, SixLabors.ImageSharp.Color.Pink, new PointF(0, 0)));
                OnMessage?.Invoke(this, textImage);
            }
            if (message.Sticker != null && message.Sticker.Type == StickerType.Regular)
            {
                MemoryStream stickerStream = new MemoryStream();
                var fileInfo = await bot.GetFile(message.Sticker.FileId);
                if (fileInfo == null || string.IsNullOrWhiteSpace(fileInfo.FilePath))
                {
                    return;
                }
                await bot.DownloadFile(fileInfo.FilePath, stickerStream);
                stickerStream.Position = 0;
                Image<Rgba32> stickerImage = SixLabors.ImageSharp.Image.Load<Rgba32>(stickerStream);


                // Calculate target size preserving aspect ratio
                double ratioX = (double)config.Telegram.StickerXSize / stickerImage.Width;
                double ratioY = (double)config.Telegram.StickerYSize / stickerImage.Height;
                double ratio = Math.Min(ratioX, ratioY); // Use the smaller ratio to fit

                int newWidth = (int)(stickerImage.Width * ratio);
                int newHeight = (int)(stickerImage.Height * ratio);


                // Resize
                stickerImage.Mutate(x => x.Resize(newWidth, newHeight));

                logger.LogInformation("Recieved a sticker message");
                OnMessage?.Invoke(this, stickerImage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to handle message [{type}]: " +
                $"Id: {message.Id}, " +
                $"Message Id: {message.MessageId}, " +
                $"From user: {message.From?.Username}");
        }
    }

    public static string WrapText(string input, int maxLineLength)
    {
        if (string.IsNullOrWhiteSpace(input) || maxLineLength <= 0)
            return string.Empty;

        var words = input.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            // If adding the word would exceed the line length
            if (currentLine.Length + word.Length + 1 > maxLineLength)
            {
                if (currentLine.Length > 0)
                {
                    result.AppendLine(currentLine.ToString().TrimEnd());
                    currentLine.Clear();
                }
            }

            currentLine.Append(word + " ");
        }

        // Append the last line if any
        if (currentLine.Length > 0)
        {
            result.AppendLine(currentLine.ToString().TrimEnd());
        }

        return result.ToString().TrimEnd(); // Trim the final newline
    }
}
