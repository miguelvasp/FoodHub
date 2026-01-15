using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Domain.Events;

public sealed record OrderSnapshot(
    Guid OrderId,
    string OrderCode,
    OrderStatus Status,
    OrderType OrderType,
    decimal OrderTotal,
    CustomerSnapshot Customer,
    RestaurantSnapshot Restaurant);
