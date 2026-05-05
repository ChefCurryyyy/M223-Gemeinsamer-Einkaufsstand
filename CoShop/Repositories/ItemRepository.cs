using Microsoft.EntityFrameworkCore;
using CoShop.Data;
using CoShop.Models;

namespace CoShop.Repositories;

public class ItemRepository : Repository<Item>, IItemRepository
{
    public ItemRepository(AppDbContext db) : base(db) { }

    public async Task<Item?> GetByIdWithUserAsync(int itemId, int listId) =>
        await _set
            .Include(i => i.LastModifiedByUser)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId);
}
