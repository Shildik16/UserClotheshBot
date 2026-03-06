using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ClothesBotUser.States;
using ClothesBotUser.Services;
using Telegram.Bot.Exceptions;

namespace ClothesBotUser.Handlers
{
    public static class CallbackHandler
    {
        public static async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserContext context, DatabaseService db, PaymentService paymentSvc, CancellationToken ct)
        {
            var data = query.Data ?? "";
            var chatId = query.Message.Chat.Id;

            // 1. Мгновенный ответ на CallbackQuery. 
            // Это убирает индикатор загрузки на кнопке и предотвращает ошибку "query is too old".
            try
            {
                await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Предупреждение: Не удалось ответить на CallbackQuery (ID: {query.Id}): {ex.Message}");
            }

            try
            {
                // Логика каталога
                if (data.StartsWith("cat_"))
                {
                    await CatalogLogic.ShowCategoryItemsAsync(bot, chatId, data.Split('_')[1], db, ct);
                }
                else if (data.StartsWith("view_"))
                {
                    await CatalogLogic.ShowProductCardAsync(bot, chatId, int.Parse(data.Split('_')[1]), db, ct);
                }
                else if (data.StartsWith("buy_"))
                {
                    context.Step = UserStep.AwaitingComment;
                    context.PendingItemId = int.Parse(data.Split('_')[1]);
                    await bot.SendMessage(chatId, "📝 Напишите ваш размер и другие пожелания к заказу:", cancellationToken: ct);
                }

                // Логика выбора оплаты
                else if (data.StartsWith("pay_"))
                {
                    int methodId = data == "pay_sbp" ? 2 : 13;
                    string methodName = methodId == 2 ? "СБП" : "Крипто";

                    string username = query.From.Username ?? "no_username";
                    
                    // Создаем запись в БД
                    int orderId = await db.CreateOrderAndGetIdAsync(chatId, username, context.PendingItemId, context.PendingComment, ct);

                    var item = await db.GetItemByIdAsync(context.PendingItemId, ct);
                    if (item == null) return;

                    // Запрос к платежной системе
                    var payment = await paymentSvc.CreatePaymentAsync(item.PriceStars, $"Заказ #{orderId}", orderId.ToString(), methodId);

                    if (payment != null && !string.IsNullOrEmpty(payment.Redirect))
                    {
                        await db.UpdateOrderExternalIdAsync(orderId, payment.TransactionId, ct);

                        var kb = new InlineKeyboardMarkup(new[] {
                            new[] { InlineKeyboardButton.WithUrl($"🔗 Оплатить через {methodName}", payment.Redirect) },
                            new[] { InlineKeyboardButton.WithCallbackData("🔄 Проверить статус оплаты", $"check_{orderId}") }
                        });

                        await SafeEditMessageText(bot, chatId, query.Message.MessageId,
                            $"✅ Заказ №{orderId} сформирован.\nСумма: {item.PriceStars} руб.\n\nНажмите кнопку ниже для перехода к оплате:",
                            kb, ct);
                    }
                    else
                    {
                        await bot.SendMessage(chatId, "⚠️ Ошибка связи с платежной системой. Попробуйте другой метод или позже.", cancellationToken: ct);
                    }
                }

                // Логика ручной проверки статуса
                else if (data.StartsWith("check_"))
                {
                    int orderId = int.Parse(data.Split('_')[1]);
                    string? extId = await db.GetExternalIdByOrderIdAsync(orderId, ct);

                    if (string.IsNullOrEmpty(extId))
                    {
                        await bot.SendMessage(chatId, "❌ Данные транзакции не найдены.", cancellationToken: ct);
                        return;
                    }

                    string? status = await paymentSvc.GetPaymentStatusAsync(extId);

                    if (status == "success" || status == "completed")
                    {
                        await db.UpdateOrderStatusAsync(orderId, "paid", ct);
                        await SafeEditMessageText(bot, chatId, query.Message.MessageId, 
                            $"🎉 Заказ №{orderId} успешно оплачен! Ожидайте сообщения от менеджера.", null, ct);
                    }
                    else
                    {
                        // Если оплата не найдена, выводим уведомление во всплывающем окне (showAlert: true)
                        await bot.AnswerCallbackQuery(query.Id, "⌛️ Оплата пока не получена. Если вы уже оплатили, подождите пару минут.", showAlert: true, cancellationToken: ct);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в обработке Callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Вспомогательный метод для безопасного редактирования сообщения.
        /// Предотвращает ошибку "Bad Request: message is not modified".
        /// </summary>
        private static async Task SafeEditMessageText(ITelegramBotClient bot, long chatId, int messageId, string text, InlineKeyboardMarkup? kb, CancellationToken ct)
        {
            try
            {
                await bot.EditMessageText(chatId, messageId, text, 
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, 
                    replyMarkup: kb, 
                    cancellationToken: ct);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                // Игнорируем ошибку, если текст сообщения идентичен старому
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при редактировании сообщения: {ex.Message}");
            }
        }
    }
}