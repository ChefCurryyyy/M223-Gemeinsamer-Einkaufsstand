namespace CoShop.Models;

public class ShoppingListMember
{
    public int UserId { get; set; }
    public int ListId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
 
    // Navigation properties
    public User User { get; set; } = null!;
    public ShoppingList ShoppingList { get; set; } = null!;
}
