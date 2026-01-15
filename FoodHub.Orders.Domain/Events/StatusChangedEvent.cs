namespace FoodHub.Orders.Domain.Events;

public sealed record StatusChangedEvent(
    OrderSnapshot Snapshot,
    OrderStatus PreviousStatus,
    OrderStatus CurrentStatus,
    DateTime OccurredAt) : IDomainEvent;
