using Microsoft.EntityFrameworkCore;
using CoShop.Data;
using CoShop.Models;

namespace CoShop.Repositories;

public class ShoppingListRepository : Repository<ShoppingList>, IShoppingListRepository
{
    public ShoppingListRepository(AppDbContext db) : base(db) { }

    public async Task<ShoppingList?> GetWithDetailsAsync(int listId) =>
        await _set
            .Include(l => l.Owner)
            .Include(l => l.Items).ThenInclude(i => i.LastModifiedByUser)
            .Include(l => l.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(l => l.Id == listId);

    public async Task<IEnumerable<ShoppingList>> GetListsForUserAsync(int userId) =>
        await _set
            .Where(l => l.OwnerId == userId || l.Members.Any(m => m.UserId == userId))
            .Include(l => l.Items)
            .Include(l => l.Members)
            .ToListAsync();

    public async Task<bool> IsOwnerOrMemberAsync(int listId, int userId) =>
        await _set.AnyAsync(l =>
            l.Id == listId &&
            (l.OwnerId == userId || l.Members.Any(m => m.UserId == userId)));
}
