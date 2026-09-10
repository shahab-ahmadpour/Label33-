using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class ProductVariant : EntityBase
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Title { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Currency { get; set; } = "IRR";
    public StockMode StockMode { get; set; } = StockMode.Tracked;
    public int? WeightGrams { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DigitalAssetId { get; set; }

    public Product Product { get; set; } = null!;
    public DigitalAsset? DigitalAsset { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public ICollection<VariantAttributeValue> AttributeValues { get; set; } = new List<VariantAttributeValue>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
