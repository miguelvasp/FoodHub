using FluentAssertions;
using FoodHub.Orders.Domain;
using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.Events;
using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Tests.Domain;

public class OrderEventsTests
{
    [Fact]
    public void Create_ShouldRaiseOrderCreatedEvent()
    {
        var order = Order.Create(
            "ORD-200",
            DateTime.UtcNow,
            new CustomerSnapshot("cust-1", "Alice"),
            new RestaurantSnapshot("rest-1", "Main Street"),
            OrderType.Delivery,
            5m,
            new List<OrderItem>
            {
                new(new ProductSnapshot("prod-1", "Burger"), 1, 10m, 0m, null)
            });

        order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedEvent);
    }

    [Fact]
    public void ChangeStatus_ToConfirmed_ShouldRaiseStatusChangedAndConfirmedEvents()
    {
        var order = BuildOrder();

        order.ChangeStatus(OrderStatus.Confirmed);

        order.DomainEvents.Should().Contain(e => e is StatusChangedEvent);
        order.DomainEvents.Should().Contain(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void Cancel_ShouldRaiseOrderCancelledEvent()
    {
        var order = BuildOrder();

        order.Cancel();

        order.DomainEvents.Should().Contain(e => e is OrderCancelledEvent);
    }

    [Fact]
    public void RemoveItem_ShouldRaiseItemRemovedEvent()
    {
        var order = BuildOrder();

        order.RemoveItemByProductId("prod-1");

        order.DomainEvents.Should().Contain(e => e is ItemRemovedEvent);
    }

    [Fact]
    public void MarkUpdated_ShouldRaiseOrderUpdatedEvent()
    {
        var order = BuildOrder();

        order.MarkUpdated();

        order.DomainEvents.Should().Contain(e => e is OrderUpdatedEvent);
    }

    private static Order BuildOrder()
    {
        return Order.Create(
            "ORD-201",
            DateTime.UtcNow,
            new CustomerSnapshot("cust-1", "Alice"),
            new RestaurantSnapshot("rest-1", "Main Street"),
            OrderType.Delivery,
            5m,
            new List<OrderItem>
            {
            new(new ProductSnapshot("prod-1", "Burger"), 1, 10m, 0m, null),
            new(new ProductSnapshot("prod-2", "Fries"), 1, 5m, 0m, null)
            });
    }
}
