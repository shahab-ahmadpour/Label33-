using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class Product : EntityBase
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public Guid? BrandId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.Physical;
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public bool IsFeatured { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public Brand? Brand { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    public ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();
    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
}
