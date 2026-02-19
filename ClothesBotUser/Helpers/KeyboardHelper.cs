using Telegram.Bot.Types.ReplyMarkups;

namespace ClothesBotUser.Helpers
{
    public static class KeyboardHelper
    {
        // Главное меню, которое всегда висит внизу
        public static ReplyKeyboardMarkup MainMenu() => new(new[]
        {
            new KeyboardButton[] { "🛍 Каталог" },
            new KeyboardButton[] { "📦 Мои заказы", "🆘 Поддержка" }
        }) 
        { 
            ResizeKeyboard = true // Чтобы кнопки были компактными
        };

        // Кнопка под карточкой товара
        public static InlineKeyboardMarkup BuyButton(int itemId) => new(new[]
        {
            new [] { InlineKeyboardButton.WithCallbackData("Купить ️", $"buy_{itemId}") }
        });
    }
}