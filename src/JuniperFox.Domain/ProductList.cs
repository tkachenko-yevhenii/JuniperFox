namespace JuniperFox.Domain;

public sealed class ProductList
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<ProductListItem> Items { get; set; } = new List<ProductListItem>();

    public ICollection<ProductListShare> Shares { get; set; } = new List<ProductListShare>();
}
