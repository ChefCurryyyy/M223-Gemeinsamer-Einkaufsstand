using CoShop.Models;

namespace CoShop.Repositories;

/// <summary>Item queries scoped to a shopping list.</summary>
public interface IItemRepository : IRepository<Item>
{
    Task<Item?> GetByIdWithUserAsync(int itemId, int listId);
}
