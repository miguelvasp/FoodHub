using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Domain.Events;

public sealed record ItemRemovedEvent(
    OrderSnapshot Snapshot,
    ProductSnapshot Product,
    DateTime OccurredAt) : IDomainEvent;
