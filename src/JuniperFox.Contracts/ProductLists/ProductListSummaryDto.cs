namespace JuniperFox.Contracts.ProductLists;

public sealed class ProductListSummaryDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
