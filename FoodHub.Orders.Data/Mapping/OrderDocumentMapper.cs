using FoodHub.Orders.Data.Documents;
using FoodHub.Orders.Domain;
using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.ValueObjects;

namespace FoodHub.Orders.Data.Mapping;

public static class OrderDocumentMapper
{
    public static OrderDocument FromDomain(Order order)
    {
        return new OrderDocument
        {
            Id = order.OrderId,
            OrderCode = order.OrderCode,
            OrderedAt = order.CreatedAt,
            Customer = new CustomerSnapshotDocument
            {
                CustomerId = order.Customer.CustomerId,
                CustomerName = order.Customer.CustomerName
            },
            Restaurant = new RestaurantSnapshotDocument
            {
                RestaurantId = order.Restaurant.RestaurantId,
                RestaurantName = order.Restaurant.RestaurantName
            },
            Items = order.Items.Select(item => new OrderItemDocument
            {
                Product = new ProductSnapshotDocument
                {
                    ProductId = item.Product.ProductId,
                    ProductDescription = item.Product.ProductDescription
                },
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                AddonsValue = item.AddonsValue,
                Notes = item.Notes,
                TotalItemValue = item.TotalItemValue
            }).ToList(),
            DeliveryFee = order.DeliveryFee,
            Coupon = order.Coupon is null
                ? null
                : new CouponDocument
                {
                    Code = order.Coupon.Code,
                    DiscountValue = order.Coupon.DiscountValue
                },
            DiscountValue = order.DiscountValue,
            TotalValue = order.OrderTotal,
            Status = order.Status,
            OrderType = order.Type,
            Version = order.Version
        };
    }

    public static Order ToDomain(OrderDocument document)
    {
        var items = document.Items.Select(item => new OrderItem(
            new ProductSnapshot(item.Product.ProductId, item.Product.ProductDescription),
            item.Quantity,
            item.UnitPrice,
            item.AddonsValue,
            item.Notes)).ToList();

        var coupon = document.Coupon is null
            ? null
            : new Coupon(document.Coupon.Code, document.Coupon.DiscountValue);

        return Order.Rehydrate(
            document.Id,
            document.OrderCode,
            document.OrderedAt,
            new CustomerSnapshot(document.Customer.CustomerId, document.Customer.CustomerName),
            new RestaurantSnapshot(document.Restaurant.RestaurantId, document.Restaurant.RestaurantName),
            document.OrderType,
            document.DeliveryFee,
            items,
            document.Status,
            document.Version,
            coupon);
    }
}
