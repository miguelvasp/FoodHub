using FoodHub.Orders.Domain;
using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.Exceptions;
using FoodHub.Orders.Domain.ValueObjects;
using FoodHub.Orders.Tests.Builders;
using FluentAssertions;

namespace FoodHub.Orders.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_ShouldCalculateOrderTotal()
    {
        var items = new List<OrderItem>
        {
            new(new ProductSnapshot("prod-1", "Burger"), 2, 10m, 1m, null),
            new(new ProductSnapshot("prod-2", "Fries"), 1, 5m, 0m, null)
        };

        var order = Order.Create(
            "ORD-100",
            DateTime.UtcNow,
            new CustomerSnapshot("cust-1", "Alice"),
            new RestaurantSnapshot("rest-1", "Main Street"),
            OrderType.Delivery,
            deliveryFee: 4m,
            items,
            new Coupon("OFF10", 3m));

        order.OrderTotal.Should().Be(27m);
    }

    [Fact]
    public void RecalculateTotals_ShouldNeverAllowNegativeTotal()
    {
        var items = new List<OrderItem>
        {
            new(new ProductSnapshot("prod-1", "Burger"), 1, 5m, 0m, null)
        };

        var order = Order.Create(
            "ORD-101",
            DateTime.UtcNow,
            new CustomerSnapshot("cust-1", "Alice"),
            new RestaurantSnapshot("rest-1", "Main Street"),
            OrderType.Delivery,
            deliveryFee: 0m,
            items,
            new Coupon("BIG", 20m));

        order.OrderTotal.Should().Be(0m);
    }

    [Fact]
    public void Cancel_ShouldNotAllowDeliveredOrders()
    {
        var order = new OrderBuilder().Build();

        order.ChangeStatus(OrderStatus.Confirmed);
        order.ChangeStatus(OrderStatus.InPreparation);
        order.ChangeStatus(OrderStatus.Ready);
        order.ChangeStatus(OrderStatus.Delivered);

        var action = () => order.Cancel();

        action.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Delivered orders cannot be cancelled.");
    }

    [Fact]
    public void ChangeStatus_ShouldRejectInvalidTransitions()
    {
        var order = new OrderBuilder().Build();

        var action = () => order.ChangeStatus(OrderStatus.Ready);

        action.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Invalid status transition from Pending to Ready.");
    }

    [Fact]
    public void UpdateDeliveryFee_ShouldRejectNegativeValues()
    {
        var order = new OrderBuilder().Build();

        var action = () => order.UpdateDeliveryFee(-1m);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("Delivery fee must be non-negative.");
    }
}
