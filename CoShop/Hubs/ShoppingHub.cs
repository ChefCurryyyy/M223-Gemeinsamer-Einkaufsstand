using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CoShop.Data;

namespace CoShop.Hubs;

[Authorize]
public class ShoppingHub : Hub
{
    private readonly AppDbContext _db;

    public ShoppingHub(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Client calls this to subscribe to real-time updates for a specific list.
    /// Server verifies the user is actually a member before adding to group.
    /// </summary>
    public async Task JoinList(int listId)
    {
        var userId = GetUserId();

        var isMember = await _db.ShoppingLists.AnyAsync(l =>
            l.Id == listId &&
            (l.OwnerId == userId || l.Members.Any(m => m.UserId == userId)));

        if (!isMember)
        {
            throw new HubException("Access denied to this list.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(listId));
    }

    /// <summary>Client calls this when navigating away from a list.</summary>
    public async Task LeaveList(int listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(listId));
    }

    public static string GroupName(int listId) => $"list-{listId}";

    private int GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? throw new HubException("Not authenticated.");
        return int.Parse(claim.Value);
    }
}