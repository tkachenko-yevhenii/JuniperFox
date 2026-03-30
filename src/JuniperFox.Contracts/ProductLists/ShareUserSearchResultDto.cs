namespace JuniperFox.Contracts.ProductLists;

public sealed class ShareUserSearchResultDto
{
    public required Guid Id { get; init; }

    public required string UserName { get; init; }
}
