public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int PriceStars { get; set; } 
    public byte[] PhotoBytes { get; set; } 
    public string Availability { get; set; }
    public int? CategoryId { get; set; }
    
    public string Category { get; set; }
}