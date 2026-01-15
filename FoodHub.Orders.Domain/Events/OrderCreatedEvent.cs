namespace FoodHub.Orders.Domain.Events;

public sealed record OrderCreatedEvent(OrderSnapshot Snapshot, DateTime OccurredAt) : IDomainEvent;
