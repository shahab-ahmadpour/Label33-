using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class ProductAttribute : EntityBase
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}

public class ProductAttributeValue : EntityBase
{
    public Guid ProductAttributeId { get; set; }
    public string Value { get; set; } = null!;
    public int SortOrder { get; set; }

    public ProductAttribute ProductAttribute { get; set; } = null!;
    public ICollection<VariantAttributeValue> VariantLinks { get; set; } = new List<VariantAttributeValue>();
}

public class VariantAttributeValue : EntityBase
{
    public Guid ProductVariantId { get; set; }
    public Guid ProductAttributeValueId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
    public ProductAttributeValue ProductAttributeValue { get; set; } = null!;
}
