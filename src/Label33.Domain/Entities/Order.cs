using Label33.Domain.Common;
using Label33.Domain.Enums;
using Label33.Domain.StateMachine;

namespace Label33.Domain.Entities;

public class Order : EntityBase
{
    public string OrderNumber { get; set; } = null!;
    public Guid UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "IRR";
    public Guid? CouponId { get; set; }
    public string? CustomerNote { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Coupon? Coupon { get; set; }
    public OrderAddress? ShippingAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
    public ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public OrderEvent TransitionTo(OrderStatus to, string message, Guid? actorUserId = null)
    {
        OrderStateMachine.EnsureCanTransition(Status, to);
        var from = Status;
        Status = to;
        UpdatedAtUtc = DateTime.UtcNow;

        if (to == OrderStatus.Paid)
            PaidAtUtc = DateTime.UtcNow;
        if (to == OrderStatus.Cancelled)
            CancelledAtUtc = DateTime.UtcNow;
        if (to == OrderStatus.Completed)
            CompletedAtUtc = DateTime.UtcNow;

        // Do not mutate Events navigation here — an empty initialized collection can be
        // treated as loaded by EF and cause spurious deletes/concurrency errors.
        return new OrderEvent
        {
            OrderId = Id,
            FromStatus = from,
            ToStatus = to,
            Message = message,
            ActorUserId = actorUserId
        };
    }
}

public class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid ProductVariantId { get; set; }
    public string ProductNameSnapshot { get; set; } = null!;
    public string SkuSnapshot { get; set; } = null!;
    public string VariantTitleSnapshot { get; set; } = null!;
    public ProductType ProductTypeSnapshot { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? DigitalAssetId { get; set; }

    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
    public DigitalEntitlement? DigitalEntitlement { get; set; }
}

public class OrderAddress : EntityBase
{
    public Guid OrderId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Province { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Line1 { get; set; } = null!;
    public string? Line2 { get; set; }

    public Order Order { get; set; } = null!;
}

public class OrderEvent : EntityBase
{
    public Guid OrderId { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string Message { get; set; } = null!;
    public Guid? ActorUserId { get; set; }

    public Order Order { get; set; } = null!;
}
