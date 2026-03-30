namespace JuniperFox.Contracts.ProductLists;

public sealed class ProductListShareMemberDto
{
    public required Guid UserId { get; init; }

    public required string UserName { get; init; }
}
