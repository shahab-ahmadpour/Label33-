using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Application.Coupons;
using Label33.Application.Inventory;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Checkout;

public sealed record CheckoutAddressDto(
    string FullName,
    string Phone,
    string Province,
    string City,
    string PostalCode,
    string Line1,
    string? Line2);

public sealed record CheckoutResult(Guid OrderId, string OrderNumber, decimal GrandTotal, IReadOnlyList<Guid> ReservationIds);

public class CheckoutService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IOrderNumberGenerator _orderNumbers;
    private readonly InventoryService _inventory;
    private readonly CouponService _coupons;

    public CheckoutService(
        IAppDbContext db,
        IClock clock,
        IOrderNumberGenerator orderNumbers,
        InventoryService inventory,
        CouponService coupons)
    {
        _db = db;
        _clock = clock;
        _orderNumbers = orderNumbers;
        _inventory = inventory;
        _coupons = coupons;
    }

    public async Task<CheckoutResult> CheckoutAsync(
        Guid cartId,
        Guid userId,
        CheckoutAddressDto address,
        string? couponCode,
        decimal shippingTotal = 0,
        CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct)
            ?? throw new DomainException("Cart not found.");

        if (cart.Status != CartStatus.Open || cart.Items.Count == 0)
            throw new DomainException("Cart is empty or not open.");

        var checkoutSessionId = Guid.NewGuid();
        var reservationIds = new List<Guid>();
        decimal subtotal = 0;

        var orderItems = new List<OrderItem>();

        foreach (var cartItem in cart.Items)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == cartItem.ProductVariantId && v.IsActive, ct)
                ?? throw new DomainException("A cart variant is no longer available.");

            if (variant.Product.Status != ProductStatus.Published)
                throw new DomainException($"Product '{variant.Product.Name}' is not published.");

            // Refresh price at checkout
            var unitPrice = variant.BasePrice;
            subtotal += unitPrice * cartItem.Quantity;

            var reservation = await _inventory.ReserveAsync(
                variant.Id,
                cartItem.Quantity,
                cart.Id,
                checkoutSessionId,
                TimeSpan.FromMinutes(20),
                ct);
            reservationIds.Add(reservation.Id);

            orderItems.Add(new OrderItem
            {
                ProductVariantId = variant.Id,
                ProductNameSnapshot = variant.Product.Name,
                SkuSnapshot = variant.Sku,
                VariantTitleSnapshot = variant.Title,
                ProductTypeSnapshot = variant.Product.ProductType,
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity,
                LineTotal = unitPrice * cartItem.Quantity,
                DigitalAssetId = variant.DigitalAssetId
            });
        }

        Guid? couponId = null;
        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            discount = await _coupons.CalculateDiscountAsync(couponCode, subtotal, ct);
            couponId = await _coupons.ValidateAndGetIdAsync(couponCode, subtotal, ct);
        }

        var order = new Order
        {
            OrderNumber = await _orderNumbers.NextAsync(ct),
            UserId = userId,
            Status = OrderStatus.Draft,
            Subtotal = subtotal,
            DiscountTotal = discount,
            ShippingTotal = shippingTotal,
            TaxTotal = 0,
            GrandTotal = Math.Max(0, subtotal - discount + shippingTotal),
            Currency = cart.Currency,
            CouponId = couponId,
            ShippingAddress = new OrderAddress
            {
                FullName = address.FullName,
                Phone = address.Phone,
                Province = address.Province,
                City = address.City,
                PostalCode = address.PostalCode,
                Line1 = address.Line1,
                Line2 = address.Line2
            }
        };

        foreach (var item in orderItems)
            order.Items.Add(item);

        var createdEvent = order.TransitionTo(OrderStatus.AwaitingPayment, "Checkout created; awaiting payment.", userId);
        _db.Orders.Add(order);
        _db.OrderEvents.Add(createdEvent);

        cart.Status = CartStatus.Converted;
        cart.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new CheckoutResult(order.Id, order.OrderNumber, order.GrandTotal, reservationIds);
    }
}
