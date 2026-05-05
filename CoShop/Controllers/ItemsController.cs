using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CoShop.Data;
using CoShop.DTOs;
using CoShop.Hubs;
using CoShop.Models;

namespace CoShop.Controllers;

[ApiController]
[Route("api/shoppinglists/{listId:int}/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ShoppingHub> _hub;

    public ItemsController(AppDbContext db, IHubContext<ShoppingHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem(int listId, [FromBody] CreateItemRequest req)
    {
        var userId = User.GetUserId();
        if (!await IsMemberOrOwner(listId, userId)) return Forbid();

        var item = new Item
        {
            Name = req.Name.Trim(), Amount = req.Amount, Unit = req.Unit.Trim(),
            ListId = listId, LastModifiedByUserId = userId,
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(i => i.LastModifiedByUser).LoadAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(listId))
            .SendAsync("ItemCreated", new ItemCreatedEvent(
                listId, item.Id, item.Name, item.Amount, item.Unit,
                userId, item.LastModifiedByUser.Username));

        return CreatedAtAction(nameof(GetItem), new { listId, id = item.Id }, MapItem(item));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetItem(int listId, int id)
    {
        var userId = User.GetUserId();
        if (!await IsMemberOrOwner(listId, userId)) return Forbid();
        var item = await _db.Items.Include(i => i.LastModifiedByUser).AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.ListId == listId);
        return item == null ? NotFound() : Ok(MapItem(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int listId, int id, [FromBody] UpdateItemRequest req)
    {
        var userId = User.GetUserId();
        if (!await IsMemberOrOwner(listId, userId)) return Forbid();

        var item = await _db.Items.Include(i => i.LastModifiedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.ListId == listId);
        if (item == null) return NotFound();

        item.Name = req.Name.Trim(); item.Amount = req.Amount; item.Unit = req.Unit.Trim();
        item.LastModifiedByUserId = userId;

        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(i => i.LastModifiedByUser).LoadAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(listId))
            .SendAsync("ItemUpdated", new ItemUpdatedEvent(
                listId, item.Id, item.Name, item.Amount, item.Unit,
                userId, item.LastModifiedByUser.Username));

        return Ok(MapItem(item));
    }

    [HttpPatch("{id:int}/bought")]
    public async Task<ActionResult<ItemDto>> ToggleBought(int listId, int id, [FromBody] ToggleBoughtRequest req)
    {
        var userId = User.GetUserId();
        if (!await IsMemberOrOwner(listId, userId)) return Forbid();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var item = await _db.Items.Include(i => i.LastModifiedByUser)
                .FirstOrDefaultAsync(i => i.Id == id && i.ListId == listId);
            if (item == null) { await tx.RollbackAsync(); return NotFound(); }

            item.IsBought = req.IsBought;
            item.LastModifiedByUserId = userId;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _db.Entry(item).Reference(i => i.LastModifiedByUser).LoadAsync();

            await _hub.Clients.Group(ShoppingHub.GroupName(listId))
                .SendAsync("ItemBoughtToggled", new ItemBoughtToggledEvent(
                    listId, item.Id, item.IsBought, userId, item.LastModifiedByUser.Username));

            return Ok(MapItem(item));
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            return Conflict(new { message = "Artikel wurde gleichzeitig geändert. Bitte neu laden." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteItem(int listId, int id)
    {
        var userId = User.GetUserId();
        if (!await IsMemberOrOwner(listId, userId)) return Forbid();

        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.ListId == listId);
        if (item == null) return NotFound();

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(listId))
            .SendAsync("ItemDeleted", new ItemDeletedEvent(listId, id, userId));

        return NoContent();
    }

    private async Task<bool> IsMemberOrOwner(int listId, int userId) =>
        await _db.ShoppingLists.AnyAsync(l =>
            l.Id == listId && (l.OwnerId == userId || l.Members.Any(m => m.UserId == userId)));

    private static ItemDto MapItem(Item i) => new(
        i.Id, i.Name, i.Amount, i.Unit, i.IsBought,
        i.ListId, i.LastModifiedByUserId, i.LastModifiedByUser?.Username ?? "");
}