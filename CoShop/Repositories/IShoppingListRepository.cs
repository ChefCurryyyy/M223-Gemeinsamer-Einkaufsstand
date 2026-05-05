using CoShop.Models;

namespace CoShop.Repositories;

/// <summary>Shopping-list queries including membership checks.</summary>
public interface IShoppingListRepository : IRepository<ShoppingList>
{
    Task<ShoppingList?> GetWithDetailsAsync(int listId);
    Task<IEnumerable<ShoppingList>> GetListsForUserAsync(int userId);
    Task<bool> IsOwnerOrMemberAsync(int listId, int userId);
}
