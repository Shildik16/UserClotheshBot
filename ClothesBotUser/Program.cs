using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using ClothesBotUser.Helpers;
using ClothesBotUser.Services;
using ClothesBotUser.Handlers; // Подключаем наши хендлеры
using ClothesBotUser.States;   // Подключаем состояния

namespace ClothesBotUser
{
    class Program
    {
        private const string Token = "8337885431:AAEX0W47H93pCypJaYgr4_soIeIiBoTHVyc";
        private const string ConnectionString = "Server=127.0.0.1;Database=shop_bot;Uid=root;Pwd=;";

        private static ITelegramBotClient _botClient;
        private static DatabaseService _dbService;

        // Словарь для хранения состояний пользователей (кто на каком шаге оформления)
        private static Dictionary<long, UserContext> _userContexts = new();

        static async Task Main(string[] args)
        {
            _botClient = new TelegramBotClient(Token);
            _dbService = new DatabaseService(ConnectionString);

            using var cts = new CancellationTokenSource();

            Console.WriteLine("Пользовательский бот запущен (исправленная версия)...");

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                new ReceiverOptions { AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>() },
                cts.Token
            );

            Console.ReadLine();
            cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            // Определяем ID чата для любого типа обновления
            long chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;
            if (chatId == 0) return;

            // Инициализируем контекст пользователя, если его еще нет
            if (!_userContexts.ContainsKey(chatId)) 
                _userContexts[chatId] = new UserContext();
            
            var context = _userContexts[chatId];

            // 1. ОБРАБОТКА ИНЛАЙН-КНОПОК (Категории, просмотр, покупка)
            if (update.CallbackQuery is { } cb) 
            {
                // ИСПРАВЛЕНИЕ: Передаем _dbService, чтобы CallbackHandler мог запрашивать товары из БД
                await CallbackHandler.HandleAsync(botClient, cb, context, _dbService, ct);
                return;
            }

            // 2. ОБРАБОТКА СООБЩЕНИЙ
            if (update.Message is not { Text: { } text }) return;

            // Если бот ждет от пользователя комментарий (размер/пожелания)
            if (context.Step == UserStep.AwaitingComment) 
            {
                string username = update.Message.From?.Username ?? "no_username";
                
                // Вызываем сохранение заказа в БД
                await _dbService.CreateOrderAsync(chatId, username, context.PendingItemId, text, ct);
                
                await botClient.SendMessage(chatId, 
                    "✅ Ваш заказ с комментарием успешно принят! Админ скоро свяжется с вами.", 
                    cancellationToken: ct);
                
                // Сбрасываем состояние пользователя
                context.Step = UserStep.None;
                return;
            }

            // 3. ОБРАБОТКА КОМАНД И ГЛАВНОГО МЕНЮ
            switch (text) 
            {
                case "/start": 
                    await botClient.SendMessage(chatId, "Добро пожаловать в наш магазин!", 
                        replyMarkup: KeyboardHelper.MainMenu(), cancellationToken: ct); 
                    break;

                case "Каталог": 
                    // ИСПРАВЛЕНИЕ: Передаем _dbService для получения списка категорий
                    await CatalogLogic.ShowCategoriesMenuAsync(botClient, chatId, _dbService, ct); 
                    break;

                case "Мои заказы":
                    await botClient.SendMessage(chatId, "Этот раздел скоро будет доступен.", cancellationToken: ct);
                    break;

                case "🆘 Поддержка": 
                    await botClient.SendMessage(chatId, "По всем вопросам: @shildik16", cancellationToken: ct); 
                    break;
            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine("Ошибка API: " + exception.Message);
            return Task.CompletedTask;
        }
    }
}