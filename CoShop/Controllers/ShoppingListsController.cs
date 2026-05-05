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
[Route("api/[controller]")]
[Authorize]
public class ShoppingListsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ShoppingHub> _hub;

    public ShoppingListsController(AppDbContext db, IHubContext<ShoppingHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShoppingListSummaryDto>>> GetMyLists()
    {
        var userId = User.GetUserId();
        var lists = await _db.ShoppingLists
            .Where(l => l.OwnerId == userId || l.Members.Any(m => m.UserId == userId))
            .Select(l => new ShoppingListSummaryDto(
                l.Id, l.Title, l.CreatedAt, l.OwnerId, l.Items.Count, l.Members.Count))
            .AsNoTracking().ToListAsync();
        return Ok(lists);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShoppingListDetailDto>> GetList(int id)
    {
        var userId = User.GetUserId();
        var list = await _db.ShoppingLists
            .Include(l => l.Owner)
            .Include(l => l.Items).ThenInclude(i => i.LastModifiedByUser)
            .Include(l => l.Members).ThenInclude(m => m.User)
            .AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);

        if (list == null) return NotFound();
        if (!IsMember(list, userId)) return Forbid();
        return Ok(MapDetail(list));
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingListSummaryDto>> CreateList([FromBody] CreateListRequest req)
    {
        var userId = User.GetUserId();
        var list = new ShoppingList { Title = req.Title.Trim(), OwnerId = userId, CreatedAt = DateTime.UtcNow };
        _db.ShoppingLists.Add(list);
        await _db.SaveChangesAsync();
        var dto = new ShoppingListSummaryDto(list.Id, list.Title, list.CreatedAt, list.OwnerId, 0, 0);
        return CreatedAtAction(nameof(GetList), new { id = list.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateList(int id, [FromBody] UpdateListRequest req)
    {
        var userId = User.GetUserId();
        var list = await _db.ShoppingLists.FindAsync(id);
        if (list == null) return NotFound();
        if (list.OwnerId != userId) return Forbid();

        list.Title = req.Title.Trim();
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(id))
            .SendAsync("ListRenamed", new ListRenamedEvent(id, list.Title, userId));

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteList(int id)
    {
        var userId = User.GetUserId();
        var list = await _db.ShoppingLists.FindAsync(id);
        if (list == null) return NotFound();
        if (list.OwnerId != userId) return Forbid();

        _db.ShoppingLists.Remove(list);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(id))
            .SendAsync("ListDeleted", new ListDeletedEvent(id, userId));

        return NoContent();
    }

    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<MemberDto>> InviteMember(int id, [FromBody] InviteMemberRequest req)
    {
        var userId = User.GetUserId();
        var list = await _db.ShoppingLists.FindAsync(id);
        if (list == null) return NotFound();
        if (list.OwnerId != userId) return Forbid();

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username.Trim());
        if (target == null) return NotFound(new { message = "Benutzer nicht gefunden." });
        if (target.Id == userId) return BadRequest(new { message = "Du bist bereits der Ersteller." });

        var already = await _db.ShoppingListMembers.AnyAsync(m => m.ListId == id && m.UserId == target.Id);
        if (already) return Conflict(new { message = "Benutzer ist bereits Mitglied." });

        var member = new ShoppingListMember { UserId = target.Id, ListId = id, JoinedAt = DateTime.UtcNow };
        _db.ShoppingListMembers.Add(member);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(id))
            .SendAsync("MemberAdded", new MemberAddedEvent(id, target.Id, target.Username));

        return Ok(new MemberDto(target.Id, target.Username, member.JoinedAt));
    }

    [HttpDelete("{id:int}/members/{memberId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int memberId)
    {
        var userId = User.GetUserId();
        var list = await _db.ShoppingLists.FindAsync(id);
        if (list == null) return NotFound();
        if (list.OwnerId != userId && userId != memberId) return Forbid();

        var member = await _db.ShoppingListMembers.FirstOrDefaultAsync(m => m.ListId == id && m.UserId == memberId);
        if (member == null) return NotFound();

        _db.ShoppingListMembers.Remove(member);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(ShoppingHub.GroupName(id))
            .SendAsync("MemberRemoved", new MemberRemovedEvent(id, memberId));

        return NoContent();
    }

    private static bool IsMember(ShoppingList l, int userId) =>
        l.OwnerId == userId || l.Members.Any(m => m.UserId == userId);

    private static ShoppingListDetailDto MapDetail(ShoppingList l) => new(
        l.Id, l.Title, l.CreatedAt, l.OwnerId, l.Owner.Username,
        l.Items.Select(i => new ItemDto(i.Id, i.Name, i.Amount, i.Unit, i.IsBought,
            i.ListId, i.LastModifiedByUserId, i.LastModifiedByUser.Username)),
        l.Members.Select(m => new MemberDto(m.UserId, m.User.Username, m.JoinedAt)));
}