using Label33.Domain.Enums;

namespace Label33.Domain.StateMachine;

public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Draft] = [OrderStatus.AwaitingPayment, OrderStatus.Cancelled],
        [OrderStatus.AwaitingPayment] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Fulfilling, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Fulfilling] = [OrderStatus.PartiallyFulfilled, OrderStatus.Completed, OrderStatus.Refunded],
        [OrderStatus.PartiallyFulfilled] = [OrderStatus.Completed, OrderStatus.Refunded],
        [OrderStatus.Completed] = [OrderStatus.Refunded],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Refunded] = []
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Order transition from '{from}' to '{to}' is not allowed.");
    }

    public static IReadOnlyCollection<OrderStatus> GetAllowedTransitions(OrderStatus from)
        => Allowed.TryGetValue(from, out var next) ? next.ToArray() : Array.Empty<OrderStatus>();
}
