using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Coupons;

public class CouponService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CouponService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<decimal> CalculateDiscountAsync(string? code, decimal subtotal, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return 0;

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct)
            ?? throw new DomainException("Coupon not found.");

        Validate(coupon, subtotal);
        return coupon.DiscountType switch
        {
            DiscountType.Percent => Math.Round(subtotal * coupon.Value / 100m, 0, MidpointRounding.AwayFromZero),
            DiscountType.FixedAmount => Math.Min(coupon.Value, subtotal),
            _ => 0
        };
    }

    public async Task<Guid> ValidateAndGetIdAsync(string code, decimal subtotal, CancellationToken ct = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct)
            ?? throw new DomainException("Coupon not found.");
        Validate(coupon, subtotal);
        return coupon.Id;
    }

    private void Validate(Domain.Entities.Coupon coupon, decimal subtotal)
    {
        var now = _clock.UtcNow;
        if (!coupon.IsActive)
            throw new DomainException("Coupon is inactive.");
        if (coupon.StartsAtUtc.HasValue && now < coupon.StartsAtUtc.Value)
            throw new DomainException("Coupon is not started yet.");
        if (coupon.EndsAtUtc.HasValue && now > coupon.EndsAtUtc.Value)
            throw new DomainException("Coupon expired.");
        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
            throw new DomainException("Coupon usage limit reached.");
        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
            throw new DomainException("Order amount is below coupon minimum.");
    }
}
