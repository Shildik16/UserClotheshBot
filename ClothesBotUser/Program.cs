using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Добавлено для Select
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClothesBotUser.Helpers;
using ClothesBotUser.Services;
using Telegram.Bot.Types.ReplyMarkups;

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
            // 1. ОБРАБОТКА НАЖАТИЙ КНОПОК (Inline)
            if (update.CallbackQuery is { } callbackQuery)
            {
                var data = callbackQuery.Data ?? "";
                var chatId = callbackQuery.Message.Chat.Id;

                if (data.StartsWith("cat_")) // Выбрана категория
                {
                    var category = data.Split('_')[1];
                    await ShowCategoryItemsAsync(chatId, category, ct);
                }
                else if (data.StartsWith("view_")) // Выбран конкретный товар из списка
                {
                    var itemId = int.Parse(data.Split('_')[1]);
                    await ShowProductCardAsync(chatId, itemId, ct);
                }
                else if (data.StartsWith("buy_")) // Оформление покупки
                {
                    var itemId = data.Split('_')[1];
                    await botClient.SendMessage(chatId, 
                        $"Вы выбрали товар №{itemId}. Начинаем оформление счета...", cancellationToken: ct);
                }
                
                // Убираем "часики" на кнопке
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            // 2. ОБРАБОТКА ТЕКСТОВЫХ КОМАНД
            if (update.Message is not { } message || message.Text is not { } messageText) return;
            var msgChatId = message.Chat.Id;

            switch (messageText)
            {
                case "/start":
                    await botClient.SendMessage(msgChatId, 
                        "Добро пожаловать! Выберите раздел в меню:", 
                        replyMarkup: KeyboardHelper.MainMenu(), cancellationToken: ct);
                    break;

                case "🛍 Каталог":
                    await ShowCategoriesMenuAsync(msgChatId, ct);
                    break;

                case "📦 Мои заказы":
                    await botClient.SendMessage(msgChatId, "Раздел в разработке.", cancellationToken: ct);
                    break;

                case "🆘 Поддержка":
                    await botClient.SendMessage(msgChatId, "Пишите: @admin_username", cancellationToken: ct);
                    break;
            }
        }

        // --- ЛОГИКА КАТАЛОГА ---

        // Шаг 1: Выбор категории
        private static async Task ShowCategoriesMenuAsync(long chatId, CancellationToken ct)
        {
            var categories = await _dbService.GetCategoriesAsync(ct); // Берем из БД!
    
            var buttons = categories.Select(c => 
                new[] { InlineKeyboardButton.WithCallbackData(c.Name, $"cat_{c.Id}") }).ToArray();

            await _botClient.SendMessage(chatId, "Выберите интересующий раздел:", 
                replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: ct);
        }
        // Шаг 2: Список товаров в категории (Текстовый список-кнопки)
        private static async Task ShowCategoryItemsAsync(long chatId, string categoryIdStr, CancellationToken ct)
        {
            int catId = int.Parse(categoryIdStr);
            var items = await _dbService.GetItemsByCategoryIdAsync(catId, ct); // Фильтр по ID категории

            if (!items.Any())
            {
                await _botClient.SendMessage(chatId, "В этом разделе пока нет товаров.", cancellationToken: ct);
                return;
            }

            var buttons = items.Select(i => 
                new[] { InlineKeyboardButton.WithCallbackData($"{i.Name} — {i.PriceStars} Stars", $"view_{i.Id}") }).ToArray();

            await _botClient.SendMessage(chatId, "Товары в этом разделе:", 
                replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: ct);
        }

        // Шаг 3: Детальная карточка товара с фото
        private static async Task ShowProductCardAsync(long chatId, int itemId, CancellationToken ct)
        {
            var item = await _dbService.GetItemByIdAsync(itemId, ct);
            string availabilityStatus = item.Availability == "in_stock" ? "✅ В наличии" : "⏳ Под заказ";
            string caption = $"<b>{item.Name}</b>\n\n" +
                            $"{item.Description}\n\n" +
                            $"Статус: {availabilityStatus}\n" +
                            $"Цена: {item.PriceStars} Stars";

            if (item.PhotoBytes != null && item.PhotoBytes.Length > 0)
            {
                using (var ms = new MemoryStream(item.PhotoBytes))
                {
                    await _botClient.SendPhoto(
                        chatId: chatId,
                        photo: InputFile.FromStream(ms),
                        caption: caption,
                        parseMode: ParseMode.Html,
                        replyMarkup: new InlineKeyboardMarkup(new[] {
                            new[] { InlineKeyboardButton.WithCallbackData("💳 Купить", $"buy_{item.Id}") },
                            new[] { InlineKeyboardButton.WithCallbackData("⬅️ К списку", $"cat_all") }
                        }),
                        cancellationToken: ct
                    );
                }
            }
            else
            {
                await _botClient.SendMessage(chatId, caption, parseMode: ParseMode.Html, 
                    replyMarkup: KeyboardHelper.BuyButton(item.Id), cancellationToken: ct);
            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine("Ошибка: " + exception.Message);
            return Task.CompletedTask;
        }
    }
}