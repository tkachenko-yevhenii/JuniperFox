namespace JuniperFox.Domain;

public sealed class ProductListShare
{
    public Guid ProductListId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset SharedAtUtc { get; set; }
}
