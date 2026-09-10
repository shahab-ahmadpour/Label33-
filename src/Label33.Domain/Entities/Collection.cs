using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class Collection : EntityBase
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? HeroImagePath { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }

    public ICollection<CollectionItem> Items { get; set; } = new List<CollectionItem>();
}

public class CollectionItem : EntityBase
{
    public Guid CollectionId { get; set; }
    public Guid ProductId { get; set; }
    public int SortOrder { get; set; }

    public Collection Collection { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
