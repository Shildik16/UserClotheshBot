public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int PriceStars { get; set; } // Было Price, лучше сделать PriceStars для ясности
    public string PhotoId { get; set; }
    public string Availability { get; set; }
}