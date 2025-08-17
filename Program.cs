using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    static readonly HashSet<long> knownUsers = new HashSet<long>();
    static long botId = 0; // тут збережемо свій ID після запуску
    static async Task Main()
    {
        var bot = new TelegramBotClient("8266271376:AAEMC2lkeHlQDS6Hy--uvIeuw9JUoBauUdk");
        // Отримуємо ID бота
        var me = await bot.GetMe();
        botId = me.Id;
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );
        Console.WriteLine($"Бот запущений: @{me.Username}");
        Console.WriteLine("Натисни Enter щоб зупинити...");
        Console.ReadLine();

        cts.Cancel();
    }
    static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } text) return;
        Console.WriteLine($"[{message.Chat.Id}] {text}");
        // Додаємо користувача в список, якщо це не бот
        if (message.From != null && message.From.Id != botId)
            knownUsers.Add(message.From.Id);

        // Меню команд
        if (text.StartsWith("/help") || text.StartsWith("/commands"))
        {
            string commands = "/all - тегнути всіх користувачів, які писали повідомлення\n" +
                              "/help або /commands - показати це меню";

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: commands,
                cancellationToken: ct
            );
            return;
        }
        // Тегання всіх
        if (text.StartsWith("/all"))
        {
            try
            {
                var chatId = message.Chat.Id;
                var sb = new StringBuilder();
                const int maxMessageLength = 4000;

                foreach (var userId in knownUsers)
                {
                    // Пропускаємо самого бота
                    if (userId == botId)
                        continue;

                    string name;
                    try
                    {
                        var member = await bot.GetChatMember(chatId, userId, ct);
                        name = !string.IsNullOrEmpty(member.User.Username)
                            ? $"@{member.User.Username}"
                            : member.User.FirstName ?? "Unknown";
                    }
                    catch
                    {
                        continue; // Пропускаємо тих, кого не вдалося отримати
                    }
                    if (sb.Length + name.Length + 1 > maxMessageLength)
                    {
                        await bot.SendMessage(
                            chatId: chatId,
                            text: sb.ToString(),
                            cancellationToken: ct
                        );
                        sb.Clear();
                    }
                    sb.Append(name + " ");
                }

                if (sb.Length > 0)
                    await bot.SendMessage(
                        chatId: chatId,
                        text: sb.ToString(),
                        cancellationToken: ct
                    );
            }
            catch (Exception ex)
            {
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Помилка: {ex.Message}",
                    cancellationToken: ct
                );
            }
        }
        else
        {
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: $"Ти написав: {text}",
                cancellationToken: ct
            );
        }
    }

    static Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Помилка polling: {ex.Message}");
        return Task.CompletedTask;
    }
}
// Цей код реалізує Telegram бота, який може тегнути всіх користувачів, які писали повідомлення в чат.