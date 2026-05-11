using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CoShop.Data;
using CoShop.DTOs;
using CoShop.Hubs;
using CoShop.Models;

namespace CoShop.Controllers;

/// <summary>CRUD-Operationen für Artikel innerhalb einer Einkaufsliste.</summary>
[ApiController]
[Route("api/shoppinglists/{listId:int}/items")]
[Authorize]
[Produces("application/json")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ShoppingHub> _hub;

    public ItemsController(AppDbContext db, IHubContext<ShoppingHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <summary>Neuen Artikel zu einer Liste hinzufügen.</summary>
    /// <param name="listId">ID der Einkaufsliste.</param>
    /// <param name="req">Name, Menge und Einheit des Artikels.</param>
    /// <response code="201">Artikel erfolgreich erstellt.</response>
    /// <response code="400">Validierungsfehler.</response>
    /// <response code="401">Nicht authentifiziert.</response>
    /// <response code="403">Benutzer ist kein Mitglied dieser Liste.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ItemDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
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

    /// <summary>Einzelnen Artikel abrufen.</summary>
    /// <param name="listId">ID der Einkaufsliste.</param>
    /// <param name="id">ID des Artikels.</param>
    /// <response code="200">ItemDto des Artikels.</response>
    /// <response code="401">Nicht authentifiziert.</response>
    /// <response code="403">Kein Zugriff auf diese Liste.</response>
    /// <response code="404">Artikel nicht gefunden.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ItemDto>> GetItem(int listId, int id)
    {
        var userId = User.GetUserId();
        if (!await IsMemberOrOwner(listId, userId)) return Forbid();
        var item = await _db.Items.Include(i => i.LastModifiedByUser).AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.ListId == listId);
        return item == null ? NotFound() : Ok(MapItem(item));
    }

    /// <summary>Artikel bearbeiten (Name, Menge, Einheit).</summary>
    /// <param name="listId">ID der Einkaufsliste.</param>
    /// <param name="id">ID des Artikels.</param>
    /// <param name="req">Neue Werte für den Artikel.</param>
    /// <response code="200">Aktualisierter ItemDto.</response>
    /// <response code="400">Validierungsfehler.</response>
    /// <response code="401">Nicht authentifiziert.</response>
    /// <response code="403">Kein Zugriff auf diese Liste.</response>
    /// <response code="404">Artikel nicht gefunden.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
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

    // KI-generiert — Transaktion + optimistisches Locking; DbUpdateConcurrencyException wird als 409 Conflict zurückgegeben
    /// <summary>
    /// Artikel als gekauft/nicht gekauft markieren.
    /// Verwendet eine Transaktion mit optimistischer Nebenläufigkeitskontrolle,
    /// um zu verhindern dass zwei Benutzer denselben Artikel gleichzeitig ändern.
    /// </summary>
    /// <param name="listId">ID der Einkaufsliste.</param>
    /// <param name="id">ID des Artikels.</param>
    /// <param name="req">Neuer IsBought-Status.</param>
    /// <response code="200">Aktualisierter ItemDto.</response>
    /// <response code="401">Nicht authentifiziert.</response>
    /// <response code="403">Kein Zugriff auf diese Liste.</response>
    /// <response code="404">Artikel nicht gefunden.</response>
    /// <response code="409">Konflikt — Artikel wurde gleichzeitig von einem anderen Benutzer geändert.</response>
    [HttpPatch("{id:int}/bought")]
    [ProducesResponseType(typeof(ItemDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
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

    /// <summary>Artikel aus der Liste löschen.</summary>
    /// <param name="listId">ID der Einkaufsliste.</param>
    /// <param name="id">ID des Artikels.</param>
    /// <response code="204">Erfolgreich gelöscht.</response>
    /// <response code="401">Nicht authentifiziert.</response>
    /// <response code="403">Kein Zugriff auf diese Liste.</response>
    /// <response code="404">Artikel nicht gefunden.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
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