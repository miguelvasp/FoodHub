namespace FoodHub.Orders.Domain.Events;

public sealed record OrderCancelledEvent(OrderSnapshot Snapshot, DateTime OccurredAt) : IDomainEvent;
