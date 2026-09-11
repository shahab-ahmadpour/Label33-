using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Application.Fulfillment;
using Label33.Application.Inventory;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Payments;

public sealed record StartPaymentResult(Guid PaymentId, string? RedirectUrl, string ProviderRef);

public class PaymentOrchestrator
{
    private readonly IAppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IClock _clock;
    private readonly InventoryService _inventory;
    private readonly FulfillmentService _fulfillment;
    private readonly IEmailSender _email;

    public PaymentOrchestrator(
        IAppDbContext db,
        IPaymentGateway gateway,
        IClock clock,
        InventoryService inventory,
        FulfillmentService fulfillment,
        IEmailSender email)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _email = email;
    }

    public async Task<StartPaymentResult> StartAsync(Guid orderId, string callbackUrl, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new DomainException("Order not found.");

        if (order.Status != OrderStatus.AwaitingPayment)
            throw new DomainException("Order is not awaiting payment.");

        var start = await _gateway.StartAsync(new PaymentStartRequest(
            order.Id,
            order.GrandTotal,
            order.Currency,
            $"33 Label order {order.OrderNumber}",
            callbackUrl), ct);

        if (!start.Succeeded)
            throw new DomainException(start.Error ?? "Payment start failed.");

        var tx = new PaymentTransaction
        {
            OrderId = order.Id,
            Provider = _gateway.ProviderName,
            ProviderRef = start.ProviderRef,
            Amount = order.GrandTotal,
            Status = PaymentStatus.Pending
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);

        return new StartPaymentResult(tx.Id, start.RedirectUrl, start.ProviderRef!);
    }

    public async Task VerifyAndCompleteAsync(string providerRef, string? rawPayload, CancellationToken ct = default)
    {
        var tx = await _db.PaymentTransactions
            .Include(p => p.Order).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(p => p.ProviderRef == providerRef, ct)
            ?? throw new DomainException("Payment transaction not found.");

        if (tx.Status == PaymentStatus.Succeeded)
            return;

        var verify = await _gateway.VerifyAsync(new PaymentVerifyRequest(providerRef, tx.Amount, rawPayload), ct);
        tx.RawCallbackPayload = rawPayload;
        tx.UpdatedAtUtc = _clock.UtcNow;

        if (!verify.Succeeded)
        {
            tx.Status = PaymentStatus.Failed;
            await ReleaseCheckoutReservationsAsync(tx.OrderId, ct);
            if (tx.Order.Status == OrderStatus.AwaitingPayment)
                _db.OrderEvents.Add(tx.Order.TransitionTo(OrderStatus.Cancelled, "Payment failed; order cancelled."));
            await _db.SaveChangesAsync(ct);
            throw new DomainException(verify.Error ?? "Payment verification failed.");
        }

        tx.Status = PaymentStatus.Succeeded;
        tx.CompletedAtUtc = _clock.UtcNow;

        if (tx.Order.Status == OrderStatus.AwaitingPayment)
            _db.OrderEvents.Add(tx.Order.TransitionTo(OrderStatus.Paid, "Payment succeeded."));

        await _db.SaveChangesAsync(ct);
        await ConsumeCheckoutReservationsAsync(tx.OrderId, ct);

        if (tx.Order.CouponId.HasValue)
        {
            var coupon = await _db.Coupons.FirstAsync(c => c.Id == tx.Order.CouponId.Value, ct);
            coupon.UsedCount += 1;
            _db.CouponRedemptions.Add(new CouponRedemption
            {
                CouponId = coupon.Id,
                OrderId = tx.OrderId,
                UserId = tx.Order.UserId,
                RedeemedAtUtc = _clock.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        await _fulfillment.StartAfterPaymentAsync(tx.OrderId, ct);
    }

    public async Task SendOrderPaidEmailAsync(Guid orderId, string? toEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return;

        var subject = $"33 Label order {order.OrderNumber}";
        var body =
            $"Payment received for order {order.OrderNumber}.\n" +
            $"Total: {order.GrandTotal:N0} {order.Currency}\n" +
            $"Status: {order.Status}\n";
        await _email.SendAsync(toEmail, subject, body, ct);
    }

    private async Task ConsumeCheckoutReservationsAsync(Guid orderId, CancellationToken ct)
    {
        var reservations = await _db.InventoryReservations
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
            await _inventory.ConsumeAsync(reservation.Id, ct);
    }

    private async Task ReleaseCheckoutReservationsAsync(Guid orderId, CancellationToken ct)
    {
        var reservations = await _db.InventoryReservations
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
            await _inventory.ReleaseAsync(reservation.Id, ct);
    }
}
