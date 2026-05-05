namespace CoShop.Models;

public class ShoppingList
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OwnerId { get; set; }
 
    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<ShoppingListMember> Members { get; set; } = new List<ShoppingListMember>();
}
