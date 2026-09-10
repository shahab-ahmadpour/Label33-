using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class Coupon : EntityBase
{
    public string Code { get; set; } = null!;
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
}

public class CouponRedemption : EntityBase
{
    public Guid CouponId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RedeemedAtUtc { get; set; } = DateTime.UtcNow;

    public Coupon Coupon { get; set; } = null!;
    public Order Order { get; set; } = null!;
}

public class SalesWindow : EntityBase
{
    public string Name { get; set; } = null!;
    public SalesWindowScope Scope { get; set; } = SalesWindowScope.Global;
    public Guid? CollectionId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Tehran";
    public bool IsEnabled { get; set; } = true;
}
