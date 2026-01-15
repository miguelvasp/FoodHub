namespace FoodHub.Orders.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
