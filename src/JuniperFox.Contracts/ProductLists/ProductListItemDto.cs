namespace JuniperFox.Contracts.ProductLists;

public sealed class ProductListItemDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required int SortOrder { get; init; }

    public string? Quantity { get; init; }

    public required bool IsPurchased { get; init; }
}
