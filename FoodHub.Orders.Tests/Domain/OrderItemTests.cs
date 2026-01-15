using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.Exceptions;
using FoodHub.Orders.Domain.ValueObjects;
using FluentAssertions;

namespace FoodHub.Orders.Tests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Constructor_ShouldCalculateTotalItemValue()
    {
        var product = new ProductSnapshot("prod-1", "Burger");

        var item = new OrderItem(product, quantity: 2, unitPrice: 10m, addonsValue: 3m, notes: "No onions");

        item.TotalItemValue.Should().Be(23m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectInvalidQuantity(int quantity)
    {
        var product = new ProductSnapshot("prod-1", "Burger");

        var action = () => new OrderItem(product, quantity, 10m, 0m, null);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("Quantity must be greater than zero.");
    }
}
