using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class Tag : EntityBase
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
}

public class ProductTag
{
    public Guid ProductId { get; set; }
    public Guid TagId { get; set; }

    public Product Product { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Guid CategoryId { get; set; }

    public Product Product { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
