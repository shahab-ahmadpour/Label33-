using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class Brand : EntityBase
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
