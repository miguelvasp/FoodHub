namespace FoodHub.Orders.Domain.Events;

public sealed record OrderUpdatedEvent(OrderSnapshot Snapshot, DateTime OccurredAt) : IDomainEvent;
