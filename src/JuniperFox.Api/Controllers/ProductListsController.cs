using System.Security.Claims;
using JuniperFox.Api.Hubs;
using JuniperFox.Contracts.ProductLists;
using JuniperFox.Domain;
using JuniperFox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JuniperFox.Api.Controllers;

[ApiController]
[Route("api/product-lists")]
[Authorize]
public sealed class ProductListsController : ControllerBase
{
    private readonly JuniperFoxDbContext _db;
    private readonly IHubContext<ListUpdatesHub> _hub;

    public ProductListsController(JuniperFoxDbContext db, IHubContext<ListUpdatesHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!);
    }

    private Task<bool> CanAccessListAsync(Guid userId, Guid listId, CancellationToken cancellationToken) =>
        _db.ProductLists
            .AsNoTracking()
            .Where(l => l.Id == listId)
            .AnyAsync(l => l.OwnerId == userId || l.Shares.Any(s => s.UserId == userId), cancellationToken);

    private Task<bool> IsOwnerAsync(Guid userId, Guid listId, CancellationToken cancellationToken) =>
        _db.ProductLists
            .AsNoTracking()
            .AnyAsync(l => l.Id == listId && l.OwnerId == userId, cancellationToken);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductListSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var ownedLists = await _db.ProductLists
            .AsNoTracking()
            .Where(l => l.OwnerId == userId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new ProductListSummaryDto
            {
                Id = l.Id,
                Title = l.Title,
                CreatedAtUtc = l.CreatedAtUtc,
                IsOwnedByCurrentUser = true,
                SharedByUserName = null,
            })
            .ToListAsync(cancellationToken);

        var sharedLists = await _db.ProductListShares
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Join(
                _db.ProductLists.AsNoTracking(),
                s => s.ProductListId,
                l => l.Id,
                (s, l) => new { l.Id, l.Title, l.CreatedAtUtc, l.OwnerId })
            .Join(
                _db.Users.AsNoTracking(),
                l => l.OwnerId,
                u => u.Id,
                (l, owner) => new ProductListSummaryDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    CreatedAtUtc = l.CreatedAtUtc,
                    IsOwnedByCurrentUser = false,
                    SharedByUserName = owner.UserName,
                })
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var lists = ownedLists
            .Concat(sharedLists)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToList();

        return Ok(lists);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductListDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var list = await _db.ProductLists
            .AsNoTracking()
            .Include(l => l.Items.OrderBy(i => i.IsPurchased).ThenBy(i => i.SortOrder))
            .FirstOrDefaultAsync(
                l => l.Id == id && (l.OwnerId == userId || l.Shares.Any(s => s.UserId == userId)),
                cancellationToken);

        if (list is null)
            return NotFound();

        return Ok(new ProductListDetailDto
        {
            Id = list.Id,
            Title = list.Title,
            CreatedAtUtc = list.CreatedAtUtc,
            Items = list.Items.Select(i => new ProductListItemDto
            {
                Id = i.Id,
                Name = i.Name,
                SortOrder = i.SortOrder,
                Quantity = i.Quantity,
                IsPurchased = i.IsPurchased,
            }).ToList(),
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProductListSummaryDto>> Create([FromBody] CreateProductListRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        if (string.IsNullOrEmpty(title))
            return BadRequest("Title is required.");

        var userId = GetUserId();
        var list = new ProductList
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Title = title,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        _db.ProductLists.Add(list);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ProductListSummaryDto
        {
            Id = list.Id,
            Title = list.Title,
            CreatedAtUtc = list.CreatedAtUtc,
            IsOwnedByCurrentUser = true,
            SharedByUserName = null,
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductListSummaryDto>> Update(Guid id, [FromBody] UpdateProductListRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        if (string.IsNullOrEmpty(title))
            return BadRequest("Title is required.");

        var userId = GetUserId();
        var list = await _db.ProductLists.FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId, cancellationToken);
        if (list is null)
            return NotFound();

        list.Title = title;
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(ListHubGroups.ForList(id)).SendAsync("ListChanged", id, cancellationToken);

        return Ok(new ProductListSummaryDto
        {
            Id = list.Id,
            Title = list.Title,
            CreatedAtUtc = list.CreatedAtUtc,
            IsOwnedByCurrentUser = true,
            SharedByUserName = null,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var list = await _db.ProductLists.FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId, cancellationToken);
        if (list is null)
            return NotFound();

        _db.ProductLists.Remove(list);
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(ListHubGroups.ForList(id)).SendAsync("ListChanged", id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<ProductListItemDto>> AddItem(Guid id, [FromBody] AddProductListItemRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Name is required.");

        var userId = GetUserId();
        if (!await CanAccessListAsync(userId, id, cancellationToken))
            return NotFound();

        var maxOrder = await _db.ProductListItems
            .Where(i => i.ProductListId == id)
            .MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? -1;

        var item = new ProductListItem
        {
            Id = Guid.NewGuid(),
            ProductListId = id,
            Name = name,
            SortOrder = maxOrder + 1,
            Quantity = request.Quantity,
            IsPurchased = false,
        };

        _db.ProductListItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(ListHubGroups.ForList(id)).SendAsync("ListChanged", id, cancellationToken);

        return Ok(new ProductListItemDto
        {
            Id = item.Id,
            Name = item.Name,
            SortOrder = item.SortOrder,
            Quantity = item.Quantity,
            IsPurchased = item.IsPurchased,
        });
    }

    [HttpPatch("{listId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ProductListItemDto>> SetItemPurchased(
        Guid listId,
        Guid itemId,
        [FromBody] SetProductListItemPurchasedRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!await CanAccessListAsync(userId, listId, cancellationToken))
            return NotFound();

        var item = await _db.ProductListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ProductListId == listId, cancellationToken);
        if (item is null)
            return NotFound();

        if (item.IsPurchased != request.IsPurchased)
        {
            if (request.IsPurchased)
            {
                var minPurchased = await _db.ProductListItems
                    .Where(i => i.ProductListId == listId && i.Id != itemId && i.IsPurchased)
                    .MinAsync(i => (int?)i.SortOrder, cancellationToken) ?? 0;
                item.SortOrder = minPurchased - 1;
            }
            else
            {
                var minOpen = await _db.ProductListItems
                    .Where(i => i.ProductListId == listId && i.Id != itemId && !i.IsPurchased)
                    .MinAsync(i => (int?)i.SortOrder, cancellationToken) ?? 0;
                item.SortOrder = minOpen - 1;
            }

            item.IsPurchased = request.IsPurchased;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(ListHubGroups.ForList(listId)).SendAsync("ListChanged", listId, cancellationToken);

        return Ok(new ProductListItemDto
        {
            Id = item.Id,
            Name = item.Name,
            SortOrder = item.SortOrder,
            Quantity = item.Quantity,
            IsPurchased = item.IsPurchased,
        });
    }

    [HttpDelete("{listId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid listId, Guid itemId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!await CanAccessListAsync(userId, listId, cancellationToken))
            return NotFound();

        var item = await _db.ProductListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ProductListId == listId, cancellationToken);
        if (item is null)
            return NotFound();

        _db.ProductListItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(ListHubGroups.ForList(listId)).SendAsync("ListChanged", listId, cancellationToken);
        return NoContent();
    }

    [HttpGet("users/search")]
    public async Task<ActionResult<IReadOnlyList<ShareUserSearchResultDto>>> SearchUsers(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var term = query.Trim();
        if (term.Length < 3)
            return Ok(Array.Empty<ShareUserSearchResultDto>());

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id != userId && u.UserName != null && EF.Functions.ILike(u.UserName, $"%{term}%"))
            .OrderBy(u => u.UserName)
            .Take(10)
            .Select(u => new ShareUserSearchResultDto
            {
                Id = u.Id,
                UserName = u.UserName!,
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("{id:guid}/shares")]
    public async Task<IActionResult> ShareList(Guid id, [FromBody] ShareProductListRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!await IsOwnerAsync(userId, id, cancellationToken))
            return NotFound();

        if (request.UserId == userId)
            return BadRequest("You already own this list.");

        var targetExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!targetExists)
            return BadRequest("User not found.");

        var alreadyShared = await _db.ProductListShares
            .AsNoTracking()
            .AnyAsync(s => s.ProductListId == id && s.UserId == request.UserId, cancellationToken);
        if (alreadyShared)
            return NoContent();

        _db.ProductListShares.Add(new ProductListShare
        {
            ProductListId = id,
            UserId = request.UserId,
            SharedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/shares")]
    public async Task<ActionResult<IReadOnlyList<ProductListShareMemberDto>>> GetShares(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!await IsOwnerAsync(userId, id, cancellationToken))
            return NotFound();

        var members = await _db.ProductListShares
            .AsNoTracking()
            .Where(s => s.ProductListId == id)
            .Join(
                _db.Users.AsNoTracking(),
                s => s.UserId,
                u => u.Id,
                (s, user) => new ProductListShareMemberDto
                {
                    UserId = user.Id,
                    UserName = user.UserName!,
                })
            .OrderBy(m => m.UserName)
            .ToListAsync(cancellationToken);

        return Ok(members);
    }

    [HttpDelete("{id:guid}/shares/{targetUserId:guid}")]
    public async Task<IActionResult> Unshare(Guid id, Guid targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!await IsOwnerAsync(userId, id, cancellationToken))
            return NotFound();

        var share = await _db.ProductListShares
            .FirstOrDefaultAsync(s => s.ProductListId == id && s.UserId == targetUserId, cancellationToken);
        if (share is null)
            return NotFound();

        _db.ProductListShares.Remove(share);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/shares/me")]
    public async Task<IActionResult> LeaveSharedList(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (await IsOwnerAsync(userId, id, cancellationToken))
            return BadRequest("Owner cannot leave own list.");

        var share = await _db.ProductListShares
            .FirstOrDefaultAsync(s => s.ProductListId == id && s.UserId == userId, cancellationToken);
        if (share is null)
            return NotFound();

        _db.ProductListShares.Remove(share);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
