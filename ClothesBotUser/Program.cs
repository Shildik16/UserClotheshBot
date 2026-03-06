using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ClothesBotUser.Helpers;
using ClothesBotUser.Services;
using ClothesBotUser.Handlers;
using ClothesBotUser.States;

namespace ClothesBotUser
{
    class Program
    {
        private const string Token = "8337885431:AAEX0W47H93pCypJaYgr4_soIeIiBoTHVyc";
        private const string ConnectionString = "Server=127.0.0.1;Database=shop_bot;Uid=root;Pwd=;";

        private static ITelegramBotClient _botClient;
        private static DatabaseService _dbService;
        private static PaymentService _paymentService;
        private static Dictionary<long, UserContext> _userContexts = new();

        static async Task Main(string[] args)
        {
            _botClient = new TelegramBotClient(Token);
            _dbService = new DatabaseService(ConnectionString);
            _paymentService = new PaymentService();

            using var cts = new CancellationTokenSource();
            Console.WriteLine("Бот запущен. Раздел заказов активен.");

            _botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, 
                new ReceiverOptions { AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>() }, cts.Token);

            Console.ReadLine();
            cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            long chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;
            if (chatId == 0) return;

            if (!_userContexts.ContainsKey(chatId)) _userContexts[chatId] = new UserContext();
            var context = _userContexts[chatId];

            if (update.CallbackQuery is { } cb)
            {
                await CallbackHandler.HandleAsync(botClient, cb, context, _dbService, _paymentService, ct);
                return;
            }

            if (update.Message is not { Text: { } text }) return;

            // Обработка ввода комментария (размера)
            if (context.Step == UserStep.AwaitingComment)
            {
                context.PendingComment = text;
                var kb = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("💳 СБП", "pay_sbp") },
                    new[] { InlineKeyboardButton.WithCallbackData("💎 Криптовалюта", "pay_crypto") }
                });
                await botClient.SendMessage(chatId, "✅ Комментарий сохранен. Выберите способ оплаты:", replyMarkup: kb, cancellationToken: ct);
                context.Step = UserStep.None;
                return;
            }

            // Главное меню
            switch (text)
            {
                case "/start": 
                    await botClient.SendMessage(chatId, "Добро пожаловать в магазин одежды!", replyMarkup: KeyboardHelper.MainMenu(), cancellationToken: ct); 
                    break;

                case "Каталог": 
                    await CatalogLogic.ShowCategoriesMenuAsync(botClient, chatId, _dbService, ct); 
                    break;

                case "Мои заказы":
                    var orders = await _dbService.GetUserOrdersAsync(chatId, ct);
                    if (orders.Count == 0)
                    {
                        await botClient.SendMessage(chatId, "📦 У вас пока нет заказов. Загляните в каталог!", cancellationToken: ct);
                    }
                    else
                    {
                        string msg = "<b>🗂 Ваши последние заказы:</b>\n\n";
                        foreach (var o in orders)
                        {
                            string status = o.Status == "paid" ? "✅ Оплачен" : "⏳ Ожидает оплаты";
                            msg += $"📦 <b>Заказ №{o.Id}</b>\n" +
                                   $"Товар: {o.ItemName}\n" +
                                   $"Сумма: {o.Price} руб.\n" +
                                   $"Статус: {status}\n" +
                                   $"Размер/Коммент: {o.Comment}\n" +
                                   "--------------------------\n";
                        }
                        await botClient.SendMessage(chatId, msg, Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
                    }
                    break;

                case "🆘 Поддержка":
                    await botClient.SendMessage(chatId, "По всем вопросам: @shildik16", cancellationToken: ct);
                    break;
            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient b, Exception e, CancellationToken c) { Console.WriteLine(e.Message); return Task.CompletedTask; }
    }
}