using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.Exceptions;
using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Domain;

public sealed class Order
{
    private static readonly IReadOnlyDictionary<OrderStatus, IReadOnlyCollection<OrderStatus>> AllowedTransitions =
        new Dictionary<OrderStatus, IReadOnlyCollection<OrderStatus>>
        {
            { OrderStatus.Pending, new[] { OrderStatus.Confirmed, OrderStatus.Cancelled } },
            { OrderStatus.Confirmed, new[] { OrderStatus.InPreparation, OrderStatus.Cancelled } },
            { OrderStatus.InPreparation, new[] { OrderStatus.Ready, OrderStatus.Cancelled } },
            { OrderStatus.Ready, new[] { OrderStatus.Delivered, OrderStatus.Cancelled } },
            { OrderStatus.Delivered, Array.Empty<OrderStatus>() },
            { OrderStatus.Cancelled, Array.Empty<OrderStatus>() }
        };

    private readonly List<OrderItem> _items;

    private Order(
        Guid orderId,
        string orderCode,
        DateTime createdAt,
        CustomerSnapshot customer,
        RestaurantSnapshot restaurant,
        OrderType type,
        decimal deliveryFee,
        Coupon? coupon,
        IEnumerable<OrderItem> items,
        OrderStatus status,
        int version)
    {
        OrderId = orderId;
        OrderCode = orderCode;
        CreatedAt = createdAt;
        Customer = customer;
        Restaurant = restaurant;
        Type = type;
        DeliveryFee = deliveryFee;
        Coupon = coupon;
        Status = status;
        Version = version;

        _items = new List<OrderItem>(items);
        RecalculateTotals();
    }

    public Guid OrderId { get; }

    public string OrderCode { get; }

    public DateTime CreatedAt { get; }

    public CustomerSnapshot Customer { get; }

    public RestaurantSnapshot Restaurant { get; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal DeliveryFee { get; private set; }

    public Coupon? Coupon { get; private set; }

    public decimal DiscountValue => CalculateDiscountValue();

    public decimal OrderTotal { get; private set; }

    public OrderStatus Status { get; private set; }

    public OrderType Type { get; private set; }

    public int Version { get; private set; }

    public static Order Create(
        string orderCode,
        DateTime createdAt,
        CustomerSnapshot customer,
        RestaurantSnapshot restaurant,
        OrderType type,
        decimal deliveryFee,
        IEnumerable<OrderItem> items,
        Coupon? coupon = null)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            throw new DomainValidationException("Order code is required.");
        }

        if (customer is null)
        {
            throw new DomainValidationException("Customer snapshot is required.");
        }

        if (restaurant is null)
        {
            throw new DomainValidationException("Restaurant snapshot is required.");
        }

        if (deliveryFee < 0)
        {
            throw new DomainValidationException("Delivery fee must be non-negative.");
        }

        if (coupon is not null && string.IsNullOrWhiteSpace(coupon.Code))
        {
            throw new DomainValidationException("Coupon code is required.");
        }

        var itemList = items?.ToList() ?? new List<OrderItem>();

        return new Order(
            Guid.NewGuid(),
            orderCode.Trim(),
            createdAt,
            customer,
            restaurant,
            type,
            deliveryFee,
            coupon,
            itemList,
            OrderStatus.Pending,
            0);
    }

    public static Order Rehydrate(
        Guid orderId,
        string orderCode,
        DateTime createdAt,
        CustomerSnapshot customer,
        RestaurantSnapshot restaurant,
        OrderType type,
        decimal deliveryFee,
        IEnumerable<OrderItem> items,
        OrderStatus status,
        int version,
        Coupon? coupon = null)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainValidationException("Order id is required.");
        }

        if (string.IsNullOrWhiteSpace(orderCode))
        {
            throw new DomainValidationException("Order code is required.");
        }

        if (customer is null)
        {
            throw new DomainValidationException("Customer snapshot is required.");
        }

        if (restaurant is null)
        {
            throw new DomainValidationException("Restaurant snapshot is required.");
        }

        if (deliveryFee < 0)
        {
            throw new DomainValidationException("Delivery fee must be non-negative.");
        }

        if (coupon is not null && string.IsNullOrWhiteSpace(coupon.Code))
        {
            throw new DomainValidationException("Coupon code is required.");
        }

        var itemList = items?.ToList() ?? new List<OrderItem>();

        return new Order(
            orderId,
            orderCode.Trim(),
            createdAt,
            customer,
            restaurant,
            type,
            deliveryFee,
            coupon,
            itemList,
            status,
            version);
    }

    public void AddItem(OrderItem item)
    {
        if (item is null)
        {
            throw new DomainValidationException("Item is required.");
        }

        _items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItemByProductId(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new DomainValidationException("Product id is required.");
        }

        var item = _items.FirstOrDefault(i => i.Product.ProductId == productId);
        if (item is null)
        {
            throw new BusinessRuleViolationException("Item not found for the product.");
        }

        _items.Remove(item);
        RecalculateTotals();
    }

    public void ApplyCoupon(Coupon coupon)
    {
        if (coupon is null)
        {
            throw new DomainValidationException("Coupon is required.");
        }

        if (string.IsNullOrWhiteSpace(coupon.Code))
        {
            throw new DomainValidationException("Coupon code is required.");
        }

        Coupon = coupon;
        RecalculateTotals();
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new BusinessRuleViolationException(
                $"Invalid status transition from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new BusinessRuleViolationException("Delivered orders cannot be cancelled.");
        }

        ChangeStatus(OrderStatus.Cancelled);
    }

    public void UpdateDeliveryFee(decimal deliveryFee)
    {
        if (deliveryFee < 0)
        {
            throw new DomainValidationException("Delivery fee must be non-negative.");
        }

        DeliveryFee = deliveryFee;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        var itemsTotal = _items.Sum(item => item.TotalItemValue);
        var baseTotal = itemsTotal + DeliveryFee;
        var total = baseTotal - DiscountValue;
        OrderTotal = total < 0 ? 0 : total;
    }

    private decimal CalculateDiscountValue()
    {
        if (Coupon is null || string.IsNullOrWhiteSpace(Coupon.Code))
        {
            return 0m;
        }

        var baseTotal = _items.Sum(item => item.TotalItemValue) + DeliveryFee;
        var discount = baseTotal * 0.10m;
        return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
    }
}
