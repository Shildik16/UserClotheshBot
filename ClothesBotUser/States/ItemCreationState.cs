namespace Clothes_Bot.Admin.States;

public enum CreationStep
{
    None,
    AwaitingName,
    AwaitingDescription,
    AwaitingPrice,
    AwaitingPhotos,
    AwaitingCategory,      // Добавлено
    AwaitingAvailability,
    AwaitingEditValue,
    AwaitingNewCategoryName // Добавлено
}

public class ItemCreationState
{
    public CreationStep Step { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int PriceStars { get; set; }
    public byte[] PhotoBytes { get; set; }
    public int? CategoryId { get; set; } // Добавлено
    public string EditingField { get; set; }
    public int? TargetItemId { get; set; }
    public long? TargetOrderId { get; set; }
}