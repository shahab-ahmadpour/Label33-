using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class CustomerProfile : EntityBase
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarPath { get; set; }
}

public class CustomerAddress : EntityBase
{
    public Guid UserId { get; set; }
    public string Label { get; set; } = "Home";
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Province { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Line1 { get; set; } = null!;
    public string? Line2 { get; set; }
    public bool IsDefault { get; set; }
}

public class WishlistItem : EntityBase
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;
}

public class ProductReview : EntityBase
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public int HelpfulCount { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductReviewVote> Votes { get; set; } = new List<ProductReviewVote>();
}

public class ProductReviewVote : EntityBase
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public bool IsHelpful { get; set; }

    public ProductReview Review { get; set; } = null!;
}
