using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Inventory;

public class InventoryService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public InventoryService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task EnsureAvailableAsync(Guid variantId, int quantity, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v => v.Id == variantId, ct)
            ?? throw new DomainException("Variant not found.");

        if (variant.StockMode == StockMode.Unlimited)
            return;

        var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.ProductVariantId == variantId, ct)
            ?? throw new DomainException("Inventory record missing for tracked variant.");

        if (item.Available < quantity)
            throw new DomainException($"Insufficient stock for SKU '{variant.Sku}'. Available: {item.Available}.");
    }

    public async Task<InventoryReservation> ReserveAsync(
        Guid variantId,
        int quantity,
        Guid? cartId,
        Guid? checkoutSessionId,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        var variant = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v => v.Id == variantId, ct)
            ?? throw new DomainException("Variant not found.");

        if (variant.StockMode == StockMode.Tracked)
        {
            var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductVariantId == variantId, ct)
                ?? throw new DomainException("Inventory record missing for tracked variant.");

            if (item.Available < quantity)
                throw new DomainException($"Insufficient stock for SKU '{variant.Sku}'.");

            item.QuantityReserved += quantity;
            item.UpdatedAtUtc = _clock.UtcNow;
        }

        var reservation = new InventoryReservation
        {
            ProductVariantId = variantId,
            Quantity = quantity,
            CartId = cartId,
            CheckoutSessionId = checkoutSessionId,
            ExpiresAtUtc = _clock.UtcNow.Add(ttl),
            Status = ReservationStatus.Active
        };
        _db.InventoryReservations.Add(reservation);
        await _db.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task ReleaseAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _db.InventoryReservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new DomainException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Active)
            return;

        var variant = await _db.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == reservation.ProductVariantId, ct);
        if (variant.StockMode == StockMode.Tracked)
        {
            var item = await _db.InventoryItems.FirstAsync(i => i.ProductVariantId == reservation.ProductVariantId, ct);
            item.QuantityReserved = Math.Max(0, item.QuantityReserved - reservation.Quantity);
            item.UpdatedAtUtc = _clock.UtcNow;
        }

        reservation.Status = ReservationStatus.Released;
        reservation.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ConsumeAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _db.InventoryReservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new DomainException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Active)
            throw new DomainException("Only active reservations can be consumed.");

        var variant = await _db.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == reservation.ProductVariantId, ct);
        if (variant.StockMode == StockMode.Tracked)
        {
            var item = await _db.InventoryItems.FirstAsync(i => i.ProductVariantId == reservation.ProductVariantId, ct);
            item.QuantityReserved = Math.Max(0, item.QuantityReserved - reservation.Quantity);
            item.QuantityOnHand = Math.Max(0, item.QuantityOnHand - reservation.Quantity);
            item.UpdatedAtUtc = _clock.UtcNow;
        }

        reservation.Status = ReservationStatus.Consumed;
        reservation.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ExpireDueReservationsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var due = await _db.InventoryReservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAtUtc <= now)
            .ToListAsync(ct);

        foreach (var reservation in due)
        {
            var variant = await _db.ProductVariants.AsNoTracking().FirstAsync(v => v.Id == reservation.ProductVariantId, ct);
            if (variant.StockMode == StockMode.Tracked)
            {
                var item = await _db.InventoryItems.FirstAsync(i => i.ProductVariantId == reservation.ProductVariantId, ct);
                item.QuantityReserved = Math.Max(0, item.QuantityReserved - reservation.Quantity);
                item.UpdatedAtUtc = now;
            }

            reservation.Status = ReservationStatus.Expired;
            reservation.UpdatedAtUtc = now;
        }

        if (due.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}
