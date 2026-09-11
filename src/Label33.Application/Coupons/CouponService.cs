using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
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

    public async Task<IReadOnlyList<Coupon>> ListAsync(CancellationToken ct = default)
        => await _db.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<Coupon> CreateAsync(
        string code,
        DiscountType discountType,
        decimal value,
        decimal? minOrderAmount,
        int? maxUses,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        CancellationToken ct = default)
    {
        code = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Coupon code is required.");
        if (value <= 0)
            throw new DomainException("Coupon value must be greater than zero.");
        if (discountType == DiscountType.Percent && value > 100)
            throw new DomainException("Percent coupon cannot exceed 100.");
        if (maxUses is <= 0)
            throw new DomainException("Max uses must be positive.");
        if (startsAtUtc.HasValue && endsAtUtc.HasValue && endsAtUtc < startsAtUtc)
            throw new DomainException("End date must be after start date.");

        if (await _db.Coupons.AnyAsync(c => c.Code == code, ct))
            throw new DomainException("Coupon code already exists.");

        var coupon = new Coupon
        {
            Code = code,
            DiscountType = discountType,
            Value = value,
            MinOrderAmount = minOrderAmount is > 0 ? minOrderAmount : null,
            MaxUses = maxUses,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            IsActive = true
        };
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);
        return coupon;
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new DomainException("Coupon not found.");
        coupon.IsActive = false;
        await _db.SaveChangesAsync(ct);
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

    private void Validate(Coupon coupon, decimal subtotal)
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
