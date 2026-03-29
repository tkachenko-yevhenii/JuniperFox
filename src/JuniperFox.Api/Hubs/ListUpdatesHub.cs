using System.Security.Claims;
using JuniperFox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JuniperFox.Api.Hubs;

[Authorize]
public sealed class ListUpdatesHub(JuniperFoxDbContext db) : Hub
{
    public async Task SubscribeToList(Guid listId)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new HubException("Unauthorized.");

        var listExists = await db.ProductLists
            .AsNoTracking()
            .AnyAsync(l => l.Id == listId && l.OwnerId == userId);

        if (!listExists)
            throw new HubException("List not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, ListHubGroups.ForList(listId));
    }

    public Task UnsubscribeFromList(Guid listId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ListHubGroups.ForList(listId));
}
