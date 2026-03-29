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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductListSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var lists = await _db.ProductLists
            .AsNoTracking()
            .Where(l => l.OwnerId == userId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new ProductListSummaryDto
            {
                Id = l.Id,
                Title = l.Title,
                CreatedAtUtc = l.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return Ok(lists);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductListDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var list = await _db.ProductLists
            .AsNoTracking()
            .Include(l => l.Items.OrderBy(i => i.IsPurchased).ThenBy(i => i.SortOrder))
            .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId, cancellationToken);

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

        return Ok(new ProductListSummaryDto
        {
            Id = list.Id,
            Title = list.Title,
            CreatedAtUtc = list.CreatedAtUtc,
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
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<ProductListItemDto>> AddItem(Guid id, [FromBody] AddProductListItemRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Name is required.");

        var userId = GetUserId();
        var list = await _db.ProductLists.FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId, cancellationToken);
        if (list is null)
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
        var list = await _db.ProductLists.FirstOrDefaultAsync(l => l.Id == listId && l.OwnerId == userId, cancellationToken);
        if (list is null)
            return NotFound();

        var item = await _db.ProductListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ProductListId == listId, cancellationToken);
        if (item is null)
            return NotFound();

        if (item.IsPurchased != request.IsPurchased)
        {
            if (request.IsPurchased)
            {
                var maxOther = await _db.ProductListItems
                    .Where(i => i.ProductListId == listId && i.Id != itemId)
                    .MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? -1;
                item.SortOrder = maxOther + 1;
            }
            else
            {
                var maxOpen = await _db.ProductListItems
                    .Where(i => i.ProductListId == listId && i.Id != itemId && !i.IsPurchased)
                    .MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? -1;
                item.SortOrder = maxOpen + 1;
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
        var list = await _db.ProductLists.FirstOrDefaultAsync(l => l.Id == listId && l.OwnerId == userId, cancellationToken);
        if (list is null)
            return NotFound();

        var item = await _db.ProductListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ProductListId == listId, cancellationToken);
        if (item is null)
            return NotFound();

        _db.ProductListItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(ListHubGroups.ForList(listId)).SendAsync("ListChanged", listId, cancellationToken);
        return NoContent();
    }
}
