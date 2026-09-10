using Label33.Domain.Common;
using Label33.Domain.Enums;

namespace Label33.Domain.Entities;

public class Cart : EntityBase
{
    public Guid? UserId { get; set; }
    public string? AnonymousToken { get; set; }
    public string Currency { get; set; } = "IRR";
    public CartStatus Status { get; set; } = CartStatus.Open;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : EntityBase
{
    public Guid CartId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;

    public Cart Cart { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}
