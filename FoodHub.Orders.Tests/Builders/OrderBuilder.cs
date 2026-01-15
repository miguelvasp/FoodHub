using FoodHub.Orders.Domain;
using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Tests.Builders;

public sealed class OrderBuilder
{
    private string _orderCode = "ORD-001";
    private DateTime _createdAt = DateTime.UtcNow;
    private CustomerSnapshot _customer = new("cust-1", "Alice");
    private RestaurantSnapshot _restaurant = new("rest-1", "Main Street");
    private OrderType _type = OrderType.Delivery;
    private decimal _deliveryFee = 5m;
    private Coupon? _coupon;
    private readonly List<OrderItem> _items =
    [
        new OrderItem(new ProductSnapshot("prod-1", "Burger"), 2, 10m, 1m, null)
    ];

    public OrderBuilder WithItems(IEnumerable<OrderItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        return this;
    }

    public OrderBuilder WithDeliveryFee(decimal deliveryFee)
    {
        _deliveryFee = deliveryFee;
        return this;
    }

    public OrderBuilder WithCoupon(Coupon coupon)
    {
        _coupon = coupon;
        return this;
    }

    public Order Build()
    {
        return Order.Create(
            _orderCode,
            _createdAt,
            _customer,
            _restaurant,
            _type,
            _deliveryFee,
            _items,
            _coupon);
    }
}
