namespace JuniperFox.Domain;

public sealed class ProductListItem
{
    public Guid Id { get; set; }

    public Guid ProductListId { get; set; }

    public ProductList ProductList { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>Optional free-form quantity or note (e.g. "15 liters, 8 pcs").</summary>
    public string? Quantity { get; set; }

    /// <summary>When true, item is shown struck through and ordered after open items.</summary>
    public bool IsPurchased { get; set; }
}
