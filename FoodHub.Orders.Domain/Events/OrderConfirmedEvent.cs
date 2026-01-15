namespace FoodHub.Orders.Domain.Events;

public sealed record OrderConfirmedEvent(OrderSnapshot Snapshot, DateTime OccurredAt) : IDomainEvent;
