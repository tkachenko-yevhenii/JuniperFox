namespace JuniperFox.Contracts.ProductLists;

public sealed class AddProductListItemRequest
{
    public required string Name { get; init; }

    public string? Quantity { get; init; }
}
