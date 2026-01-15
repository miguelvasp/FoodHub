namespace FoodHub.Orders.Domain;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    InPreparation = 2,
    Ready = 3,
    Delivered = 4,
    Cancelled = 5
}
