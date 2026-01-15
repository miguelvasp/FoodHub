using System.ComponentModel.DataAnnotations;
using FoodHub.Orders.Domain;

namespace FoodHub.Orders.Api.Contracts;

public sealed record CreateOrderRequest(
    [property: Required] string Code,
    [property: Required] DateTime OrderedAt,
    [property: Required] CustomerSnapshotRequest Customer,
    [property: Required] RestaurantSnapshotRequest Restaurant,
    [property: Required, MinLength(1)] List<OrderItemRequest> Items,
    [property: Range(0, double.MaxValue)] decimal DeliveryFee,
    string? CouponCode,
    [property: Required] OrderType OrderType);

public sealed record UpdateOrderRequest(
    [property: Required] DateTime OrderedAt,
    [property: Required] CustomerSnapshotRequest Customer,
    [property: Required] RestaurantSnapshotRequest Restaurant,
    [property: Required, MinLength(1)] List<OrderItemRequest> Items,
    [property: Range(0, double.MaxValue)] decimal DeliveryFee,
    string? CouponCode,
    [property: Required] OrderType OrderType,
    [property: Range(0, int.MaxValue)] int Version);

public sealed record OrderItemRequest(
    [property: Required] ProductSnapshotRequest Product,
    [property: Range(1, int.MaxValue)] int Quantity,
    [property: Range(0, double.MaxValue)] decimal UnitPrice,
    string? Notes,
    [property: Range(0, double.MaxValue)] decimal AddonsValue);

public sealed record CustomerSnapshotRequest(
    [property: Required] string Id,
    [property: Required] string Name);

public sealed record RestaurantSnapshotRequest(
    [property: Required] string Id,
    [property: Required] string Name);

public sealed record ProductSnapshotRequest(
    [property: Required] string Id,
    [property: Required] string Description);
