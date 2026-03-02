using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClothesBotUser.Services;
using ClothesBotUser.Models; // Твоя модель Item здесь
namespace ClothesBotUser.Handlers;

public static class CatalogLogic
{
    // Показываем меню категорий
    public static async Task ShowCategoriesMenuAsync(ITelegramBotClient bot, long chatId, DatabaseService db, CancellationToken ct)
    {
        var categories = await db.GetCategoriesAsync(ct);
        var buttons = categories.Select(c => 
            new[] { InlineKeyboardButton.WithCallbackData(c.Name, $"cat_{c.Id}") }).ToArray();

        await bot.SendMessage(chatId, "Выберите интересующий раздел:", 
            replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: ct);
    }

    // Показываем список товаров в категории
    public static async Task ShowCategoryItemsAsync(ITelegramBotClient bot, long chatId, string catIdStr, DatabaseService db, CancellationToken ct)
    {
        int catId = int.Parse(catIdStr);
        var items = await db.GetItemsByCategoryIdAsync(catId, ct);

        if (!items.Any())
        {
            await bot.SendMessage(chatId, "В этом разделе пока нет товаров.", cancellationToken: ct);
            return;
        }

        // Используем i.PriceStars из твоей модели
        var buttons = items.Select(i => 
            new[] { InlineKeyboardButton.WithCallbackData($"{i.Name} — {i.PriceStars} ", $"view_{i.Id}") }).ToArray();

        await bot.SendMessage(chatId, "Товары в этом разделе:", 
            replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: ct);
    }

    // Показываем карточку конкретного товара
    public static async Task ShowProductCardAsync(ITelegramBotClient bot, long chatId, int itemId, DatabaseService db, CancellationToken ct)
    {
        var item = await db.GetItemByIdAsync(itemId, ct);
        if (item == null) return;

        string availabilityStatus = item.Availability == "in_stock" ? "✅ В наличии" : "⏳ Под заказ";
        string caption = $"<b>{item.Name}</b>\n\n" +
                        $"{item.Description}\n\n" +
                        $"Статус: {availabilityStatus}\n" +
                        $"Цена: {item.PriceStars}";

        if (item.PhotoBytes != null && item.PhotoBytes.Length > 0)
        {
            using var ms = new MemoryStream(item.PhotoBytes);
            await bot.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromStream(ms),
                caption: caption,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("💳 Купить", $"buy_{item.Id}") },
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ К списку", $"cat_{item.CategoryId}") }
                }),
                cancellationToken: ct
            );
        }
        else
        {
            await bot.SendMessage(chatId, caption, parseMode: ParseMode.Html, 
                replyMarkup: new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("💳 Купить", $"buy_{item.Id}") }
                }), cancellationToken: ct);
        }
    }
}