using Label33.Domain.Enums;
using Label33.Domain.StateMachine;

namespace Label33.Tests.Domain;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.AwaitingPayment, true)]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Fulfilling, true)]
    [InlineData(OrderStatus.Fulfilling, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Draft, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Completed, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    public void CanTransition_matches_rules(OrderStatus from, OrderStatus to, bool expected)
        => Assert.Equal(expected, OrderStateMachine.CanTransition(from, to));

    [Fact]
    public void EnsureCanTransition_throws_on_illegal()
        => Assert.Throws<InvalidOperationException>(() =>
            OrderStateMachine.EnsureCanTransition(OrderStatus.Draft, OrderStatus.Completed));
}
