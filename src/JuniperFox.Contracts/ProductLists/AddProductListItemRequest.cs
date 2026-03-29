namespace JuniperFox.Contracts.ProductLists;

public sealed class AddProductListItemRequest
{
    public required string Name { get; init; }

    public int? Quantity { get; init; }
}
