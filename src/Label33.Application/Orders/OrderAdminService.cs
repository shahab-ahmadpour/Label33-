using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Label33.Domain.StateMachine;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Orders;

public class OrderAdminService
{
    private readonly IAppDbContext _db;

    public OrderAdminService(IAppDbContext db) => _db = db;

    public async Task TransitionAsync(Guid orderId, OrderStatus to, string? note, Guid? actorUserId, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new DomainException("Order not found.");

        var message = string.IsNullOrWhiteSpace(note)
            ? $"Status changed to {to}."
            : note.Trim();

        _db.OrderEvents.Add(order.TransitionTo(to, message, actorUserId));
        await _db.SaveChangesAsync(ct);
    }

    public IReadOnlyCollection<OrderStatus> GetAllowedTransitions(OrderStatus from)
        => OrderStateMachine.GetAllowedTransitions(from);
}
