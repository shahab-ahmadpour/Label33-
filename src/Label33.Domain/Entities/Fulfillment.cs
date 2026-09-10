using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class PaymentTransaction : EntityBase
{
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = null!;
    public string? ProviderRef { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? RawCallbackPayload { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}

public class Shipment : EntityBase
{
    public Guid OrderId { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingCode { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Preparing;
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}

public class DigitalEntitlement : EntityBase
{
    public Guid OrderItemId { get; set; }
    public Guid UserId { get; set; }
    public Guid DigitalAssetId { get; set; }
    public int DownloadsUsed { get; set; }
    public int? MaxDownloads { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public DigitalAsset DigitalAsset { get; set; } = null!;
    public ICollection<PurchaseAccessToken> AccessTokens { get; set; } = new List<PurchaseAccessToken>();
}

public class PurchaseAccessToken : EntityBase
{
    public Guid EntitlementId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public bool IsRevoked { get; set; }

    public DigitalEntitlement Entitlement { get; set; } = null!;
}
