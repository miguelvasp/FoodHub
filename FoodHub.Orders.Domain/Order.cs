using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.Events;
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
    private readonly List<IDomainEvent> _domainEvents = new();

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

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

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

        // Invariante de criação: um pedido precisa ter ao menos 1 item.
        if (itemList.Count == 0)
        {
            throw new DomainValidationException("Order must contain at least one item.");
        }

        if (itemList.Any(i => i is null))
        {
            throw new DomainValidationException("Order items cannot be null.");
        }

        var order = new Order(
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

        order.AddDomainEvent(new OrderCreatedEvent(order.CreateSnapshot(), DateTime.UtcNow));
        return order;
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

        if (version < 0)
        {
            throw new DomainValidationException("Version must be non-negative.");
        }

        if (coupon is not null && string.IsNullOrWhiteSpace(coupon.Code))
        {
            throw new DomainValidationException("Coupon code is required.");
        }

        var itemList = items?.ToList() ?? new List<OrderItem>();

        if (itemList.Any(i => i is null))
        {
            throw new DomainValidationException("Order items cannot be null.");
        }

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

        EnsureOrderIsModifiable();

        _items.Add(item);
        RecalculateTotals();

        IncrementVersion();
        MarkUpdated();
    }

    public void RemoveItemByProductId(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new DomainValidationException("Product id is required.");
        }

        EnsureOrderIsModifiable();

        // Evita deixar o pedido em um estado inválido (sem itens).
        if (_items.Count == 1)
        {
            var onlyItem = _items[0];
            if (onlyItem.Product.ProductId == productId)
            {
                throw new BusinessRuleViolationException("Order cannot have zero items.");
            }
        }

        var item = _items.FirstOrDefault(i => i.Product.ProductId == productId);
        if (item is null)
        {
            throw new BusinessRuleViolationException("Item not found for the product.");
        }

        _items.Remove(item);
        AddDomainEvent(new ItemRemovedEvent(CreateSnapshot(), item.Product, DateTime.UtcNow));
        RecalculateTotals();

        IncrementVersion();
        MarkUpdated();
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

        EnsureOrderIsModifiable();

        Coupon = coupon;
        RecalculateTotals();

        IncrementVersion();
        MarkUpdated();
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

        var previousStatus = Status;
        Status = newStatus;

        IncrementVersion();
        AddDomainEvent(new StatusChangedEvent(CreateSnapshot(), previousStatus, newStatus, DateTime.UtcNow));

        if (newStatus == OrderStatus.Confirmed)
        {
            AddDomainEvent(new OrderConfirmedEvent(CreateSnapshot(), DateTime.UtcNow));
        }

        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new BusinessRuleViolationException("Delivered orders cannot be cancelled.");
        }

        ChangeStatus(OrderStatus.Cancelled);
        AddDomainEvent(new OrderCancelledEvent(CreateSnapshot(), DateTime.UtcNow));
    }

    public void UpdateDeliveryFee(decimal deliveryFee)
    {
        if (deliveryFee < 0)
        {
            throw new DomainValidationException("Delivery fee must be non-negative.");
        }

        EnsureOrderIsModifiable();

        if (deliveryFee == DeliveryFee)
        {
            return;
        }

        DeliveryFee = deliveryFee;
        RecalculateTotals();

        IncrementVersion();
        MarkUpdated();
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

    public void MarkUpdated()
    {
        AddDomainEvent(new OrderUpdatedEvent(CreateSnapshot(), DateTime.UtcNow));
    }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
        {
            throw new DomainValidationException("Domain event is required.");
        }

        _domainEvents.Add(domainEvent);
    }

    private OrderSnapshot CreateSnapshot()
    {
        return new OrderSnapshot(
            OrderId,
            OrderCode,
            Status,
            Type,
            OrderTotal,
            Customer,
            Restaurant);
    }

    private void IncrementVersion()
    {
        checked
        {
            Version++;
        }
    }

    private void EnsureOrderIsModifiable()
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new BusinessRuleViolationException("Delivered orders cannot be modified.");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new BusinessRuleViolationException("Cancelled orders cannot be modified.");
        }
    }
}
