namespace CoShop.Models;

public enum UserRole
{
    /// <summary>Standard user — can create and manage their own lists.</summary>
    User = 0,
    /// <summary>Administrator — can view all lists and manage all users.</summary>
    Admin = 1
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role controls access to admin endpoints.</summary>
    public UserRole Role { get; set; } = UserRole.User;

    // Navigation properties
    public ICollection<ShoppingList> OwnedLists { get; set; } = new List<ShoppingList>();
    public ICollection<ShoppingListMember> SharedLists { get; set; } = new List<ShoppingListMember>();
    public ICollection<Item> LastModifiedItems { get; set; } = new List<Item>();
}