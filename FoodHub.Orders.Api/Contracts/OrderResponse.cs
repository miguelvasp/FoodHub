using FoodHub.Orders.Domain;

namespace FoodHub.Orders.Api.Contracts;

public sealed record OrderResponse(
    string Id,
    string Code,
    DateTime OrderedAt,
    CustomerSnapshotResponse Customer,
    RestaurantSnapshotResponse Restaurant,
    IReadOnlyList<OrderItemResponse> Items,
    decimal DeliveryFee,
    string? CouponCode,
    decimal DiscountValue,
    decimal TotalValue,
    OrderStatus Status,
    OrderType OrderType,
    int Version);

public sealed record OrderItemResponse(
    string Id,
    ProductSnapshotResponse Product,
    int Quantity,
    decimal UnitPrice,
    string? Notes,
    decimal AddonsValue,
    decimal TotalItemValue);

public sealed record CustomerSnapshotResponse(string Id, string Name);

public sealed record RestaurantSnapshotResponse(string Id, string Name);

public sealed record ProductSnapshotResponse(string Id, string Description);
