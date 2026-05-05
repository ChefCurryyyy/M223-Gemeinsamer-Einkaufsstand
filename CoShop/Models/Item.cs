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

    // SQL Server native optimistic concurrency — auto-incremented by the DB on every UPDATE.
    // EF Core maps this to a rowversion / timestamp column.
    // No manual setting needed: the DB handles it, unlike the SQLite long workaround.
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public ShoppingList List { get; set; } = null!;
    public User LastModifiedByUser { get; set; } = null!;
}