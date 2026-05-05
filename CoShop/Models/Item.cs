namespace CoShop.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsBought { get; set; } = false;
    public int ListId { get; set; }
    public int LastModifiedByUserId { get; set; }

    // Optimistic concurrency token (SQLite-compatible)
    public long Version { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // Navigation properties
    public ShoppingList List { get; set; } = null!;
    public User LastModifiedByUser { get; set; } = null!;
}