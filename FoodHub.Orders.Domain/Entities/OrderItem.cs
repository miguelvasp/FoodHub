using FoodHub.Orders.Domain.Exceptions;
using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Domain.Entities;

public sealed class OrderItem
{
    public OrderItem(ProductSnapshot product, int quantity, decimal unitPrice, decimal addonsValue, string? notes)
    {
        if (product is null)
        {
            throw new DomainValidationException("Product snapshot is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainValidationException("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainValidationException("Unit price must be non-negative.");
        }

        if (addonsValue < 0)
        {
            throw new DomainValidationException("Addons value must be non-negative.");
        }

        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
        AddonsValue = addonsValue;
        Notes = notes;
        TotalItemValue = CalculateTotal();
    }

    public ProductSnapshot Product { get; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal AddonsValue { get; private set; }

    public string? Notes { get; private set; }

    public decimal TotalItemValue { get; private set; }

    public void Update(int quantity, decimal unitPrice, decimal addonsValue, string? notes)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainValidationException("Unit price must be non-negative.");
        }

        if (addonsValue < 0)
        {
            throw new DomainValidationException("Addons value must be non-negative.");
        }

        Quantity = quantity;
        UnitPrice = unitPrice;
        AddonsValue = addonsValue;
        Notes = notes;
        TotalItemValue = CalculateTotal();
    }

    private decimal CalculateTotal()
    {
        return Quantity * UnitPrice + AddonsValue;
    }
}
