using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class InventoryItem
{
    public Guid ProductVariantId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int? ReorderLevel { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public int Available => QuantityOnHand - QuantityReserved;

    public ProductVariant ProductVariant { get; set; } = null!;
}

public class InventoryReservation : EntityBase
{
    public Guid? CartId { get; set; }
    public Guid? CheckoutSessionId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    public ProductVariant ProductVariant { get; set; } = null!;
    public Cart? Cart { get; set; }
    public Order? Order { get; set; }
}
