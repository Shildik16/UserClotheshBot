using Telegram.Bot;
using Telegram.Bot.Types;
using ClothesBotUser.States;
using ClothesBotUser.Services;

namespace ClothesBotUser.Handlers;

public static class CallbackHandler
{
    public static async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserContext context, DatabaseService db, CancellationToken ct)
    {
        var data = query.Data ?? "";
        var chatId = query.Message.Chat.Id;

        if (data.StartsWith("cat_")) 
        {
            await CatalogLogic.ShowCategoryItemsAsync(bot, chatId, data.Split('_')[1], db, ct);
        }
        else if (data.StartsWith("view_")) 
        {
            // Исправлено: теперь передаем db, чтобы запрос выполнился
            await CatalogLogic.ShowProductCardAsync(bot, chatId, int.Parse(data.Split('_')[1]), db, ct);
        }
        else if (data.StartsWith("buy_"))
        {
            context.Step = UserStep.AwaitingComment;
            context.PendingItemId = int.Parse(data.Split('_')[1]);
            await bot.SendMessage(chatId, "📝 Пожалуйста, напишите ваш размер и пожелания к заказу:", 
                Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
        }

        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
    }
}