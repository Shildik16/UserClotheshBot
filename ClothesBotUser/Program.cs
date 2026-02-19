using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClothesBotUser.Helpers;
using ClothesBotUser.Services;

namespace ClothesBotUser
{
    class Program
    {
        private const string Token = "8337885431:AAEX0W47H93pCypJaYgr4_soIeIiBoTHVyc";
        private const string ConnectionString = "Server=127.0.0.1;Database=shop_bot;Uid=root;Pwd=;";

        private static ITelegramBotClient _botClient;
        private static DatabaseService _dbService;

        static async Task Main(string[] args)
        {
            _botClient = new TelegramBotClient(Token);
            _dbService = new DatabaseService(ConnectionString);

            using var cts = new CancellationTokenSource();

            Console.WriteLine("Пользовательский бот запущен...");

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
                cts.Token
            );

            Console.ReadLine();
            cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            // 1. ОБРАБОТКА НАЖАТИЙ ПОД ТОВАРОМ (Callback)
            if (update.CallbackQuery is { } callbackQuery)
            {
                if (callbackQuery.Data.StartsWith("buy_"))
                {
                    var itemId = callbackQuery.Data.Split('_')[1];
                    // Здесь будет логика выставления счета Stars
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, 
                        $"Вы выбрали товар №{itemId}. Начинаем оформление счета...", cancellationToken: ct);
                }
                return;
            }

            // 2. ОБРАБОТКА ТЕКСТОВЫХ СООБЩЕНИЙ
            if (update.Message is not { } message || message.Text is not { } messageText) return;

            var chatId = message.Chat.Id;

            switch (messageText)
            {
                case "/start":
                    await botClient.SendMessage(chatId, 
                        "Добро пожаловать в наш магазин одежды! Выберите раздел в меню:", 
                        replyMarkup: KeyboardHelper.MainMenu(), cancellationToken: ct);
                    break;

                case "🛍 Каталог":
                    await ShowCatalogAsync(chatId, ct);
                    break;

                case "📦 Мои заказы":
                    await botClient.SendMessage(chatId, "Раздел в разработке. Скоро вы сможете видеть статус ваших посылок.", cancellationToken: ct);
                    break;

                case "🆘 Поддержка":
                    await botClient.SendMessage(chatId, "По всем вопросам пишите: @admin_username", cancellationToken: ct);
                    break;
            }
        }

        private static async Task ShowCatalogAsync(long chatId, CancellationToken ct)
        {
            var items = await _dbService.GetAllItemsAsync(ct);

            if (items.Count == 0)
            {
                await _botClient.SendMessage(chatId, "К сожалению, каталог пока пуст.", cancellationToken: ct);
                return;
            }

            foreach (var item in items)
            {
                // Формируем статус наличия для пользователя
                string availabilityStatus = item.Availability == "in_stock" ? "✅ В наличии" : "⏳ Под заказ";

                string caption = $"**{item.Name}**\n\n" +
                                $"{item.Description}\n\n" +
                                $"Статус: {availabilityStatus}\n" +
                                $"Цена: {item.PriceStars} ";

                // Отправляем фото с кнопкой покупки
                await _botClient.SendPhoto(
                    chatId: chatId,
                    photo: item.PhotoId, // Если item.PhotoId это строка с FileId
                    caption: caption,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: KeyboardHelper.BuyButton(item.Id),
                    cancellationToken: ct
                );
            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine("Ошибка: " + exception.Message);
            return Task.CompletedTask;
        }
    }
}