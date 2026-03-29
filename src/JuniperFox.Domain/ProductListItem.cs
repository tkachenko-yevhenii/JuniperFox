namespace JuniperFox.Domain;

public sealed class ProductListItem
{
    public Guid Id { get; set; }

    public Guid ProductListId { get; set; }

    public ProductList ProductList { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>Optional quantity or note encoded as integer later; nullable for flexibility.</summary>
    public int? Quantity { get; set; }

    /// <summary>When true, item is shown struck through and ordered after open items.</summary>
    public bool IsPurchased { get; set; }
}
