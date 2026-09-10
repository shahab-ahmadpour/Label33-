using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Label33.Application.Fulfillment;

public class FulfillmentService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public FulfillmentService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task StartAfterPaymentAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new DomainException("Order not found.");

        if (order.Status == OrderStatus.Paid)
            _db.OrderEvents.Add(order.TransitionTo(OrderStatus.Fulfilling, "Fulfillment started."));

        foreach (var item in order.Items.Where(i => i.ProductTypeSnapshot == ProductType.Digital && i.DigitalAssetId.HasValue))
        {
            var asset = await _db.DigitalAssets.FirstAsync(a => a.Id == item.DigitalAssetId!.Value, ct);
            _db.DigitalEntitlements.Add(new DigitalEntitlement
            {
                OrderItemId = item.Id,
                UserId = order.UserId,
                DigitalAssetId = asset.Id,
                MaxDownloads = asset.MaxDownloads,
                DownloadsUsed = 0
            });
        }

        var hasPhysical = order.Items.Any(i => i.ProductTypeSnapshot == ProductType.Physical);
        var onlyDigital = order.Items.All(i => i.ProductTypeSnapshot == ProductType.Digital);

        if (hasPhysical)
        {
            _db.Shipments.Add(new Shipment
            {
                OrderId = order.Id,
                Status = ShipmentStatus.Preparing
            });
        }

        if (onlyDigital)
            _db.OrderEvents.Add(order.TransitionTo(OrderStatus.Completed, "Digital order fulfilled automatically."));

        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkShippedAsync(Guid shipmentId, string? carrier, string? trackingCode, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments.Include(s => s.Order).FirstOrDefaultAsync(s => s.Id == shipmentId, ct)
            ?? throw new DomainException("Shipment not found.");

        shipment.Carrier = carrier;
        shipment.TrackingCode = trackingCode;
        shipment.Status = ShipmentStatus.Shipped;
        shipment.ShippedAtUtc = _clock.UtcNow;
        shipment.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkDeliveredAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments.Include(s => s.Order).ThenInclude(o => o.Shipments)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, ct)
            ?? throw new DomainException("Shipment not found.");

        shipment.Status = ShipmentStatus.Delivered;
        shipment.DeliveredAtUtc = _clock.UtcNow;
        shipment.UpdatedAtUtc = _clock.UtcNow;

        var allDelivered = shipment.Order.Shipments.All(s => s.Status == ShipmentStatus.Delivered || s.Id == shipment.Id);
        if (allDelivered && (shipment.Order.Status == OrderStatus.Fulfilling || shipment.Order.Status == OrderStatus.PartiallyFulfilled))
            _db.OrderEvents.Add(shipment.Order.TransitionTo(OrderStatus.Completed, "All shipments delivered."));

        await _db.SaveChangesAsync(ct);
    }
}

public class AccessTokenService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AccessTokenService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<(string PlainToken, PurchaseAccessToken Entity)> IssueAsync(Guid entitlementId, TimeSpan ttl, CancellationToken ct = default)
    {
        var entitlement = await _db.DigitalEntitlements.FirstOrDefaultAsync(e => e.Id == entitlementId, ct)
            ?? throw new DomainException("Entitlement not found.");

        if (entitlement.ExpiresAtUtc.HasValue && entitlement.ExpiresAtUtc.Value < _clock.UtcNow)
            throw new DomainException("Entitlement expired.");

        if (entitlement.MaxDownloads.HasValue && entitlement.DownloadsUsed >= entitlement.MaxDownloads.Value)
            throw new DomainException("Download limit reached.");

        var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var entity = new PurchaseAccessToken
        {
            EntitlementId = entitlementId,
            TokenHash = Hash(plain),
            ExpiresAtUtc = _clock.UtcNow.Add(ttl)
        };
        _db.PurchaseAccessTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (plain, entity);
    }

    public async Task<DigitalEntitlement> ConsumeAsync(string plainToken, CancellationToken ct = default)
    {
        var hash = Hash(plainToken);
        var token = await _db.PurchaseAccessTokens
            .Include(t => t.Entitlement).ThenInclude(e => e.DigitalAsset)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new DomainException("Invalid access token.");

        if (token.IsRevoked)
            throw new DomainException("Access token revoked.");
        if (token.UsedAtUtc.HasValue)
            throw new DomainException("Access token already used.");
        if (token.ExpiresAtUtc < _clock.UtcNow)
            throw new DomainException("Access token expired.");

        token.UsedAtUtc = _clock.UtcNow;
        token.Entitlement.DownloadsUsed += 1;
        token.Entitlement.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return token.Entitlement;
    }

    private static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }
}
