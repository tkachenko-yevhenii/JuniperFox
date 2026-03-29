namespace JuniperFox.Contracts.ProductLists;

public sealed class ProductListDetailDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required IReadOnlyList<ProductListItemDto> Items { get; init; }
}
