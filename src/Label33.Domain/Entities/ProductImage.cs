using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class ProductImage : EntityBase
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string PathOrUrl { get; set; } = null!;
    public int SortOrder { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
