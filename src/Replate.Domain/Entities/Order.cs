using Replate.Domain.Enums;

namespace Replate.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public string PickupCode { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Reserved;
    public TimeSpan PreparationDuration { get; private set; }
    public DateTime EstimatedReadyTime { get; private set; }
    public DateTime OrderedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public List<OrderItem> Items { get; } = [];

    public void Confirm() => Transition(OrderStatus.Reserved, OrderStatus.Confirmed);

    public void SetPreparationTime(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        PreparationDuration = duration;
        EstimatedReadyTime = CalculateEstimatedReadyTime();
    }

    public DateTime CalculateEstimatedReadyTime() => OrderedAt + PreparationDuration;

    public void MarkReady() => Transition(OrderStatus.Confirmed, OrderStatus.ReadyForPickup);

    public void Complete()
    {
        Transition(OrderStatus.ReadyForPickup, OrderStatus.Completed);
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel an order with status {Status}.");
        }

        Status = OrderStatus.Cancelled;
    }

    private void Transition(OrderStatus expected, OrderStatus next)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot change order status from {Status} to {next}.");
        }

        Status = next;
    }
}
