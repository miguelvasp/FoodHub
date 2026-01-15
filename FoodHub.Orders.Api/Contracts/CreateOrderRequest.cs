using FoodHub.Orders.Domain;
using System.ComponentModel.DataAnnotations;

namespace FoodHub.Orders.Api.Contracts;

public sealed record CreateOrderRequest(
    [Required] string Code,
    [Required] DateTime OrderedAt,
    [Required] CustomerSnapshotRequest Customer,
    [Required] RestaurantSnapshotRequest Restaurant,
    [Required, MinLength(1)] List<OrderItemRequest> Items,
    [Range(0, double.MaxValue)] decimal DeliveryFee,
    string? CouponCode,
    [Required] OrderType OrderType);

public sealed record UpdateOrderRequest(
    [Required] DateTime OrderedAt,
    [Required] CustomerSnapshotRequest Customer,
    [Required] RestaurantSnapshotRequest Restaurant,
    [Required, MinLength(1)] List<OrderItemRequest> Items,
    [Range(0, double.MaxValue)] decimal DeliveryFee,
    string? CouponCode,
    [Required] OrderType OrderType,
    [Range(0, int.MaxValue)] int Version);

public sealed record CancelOrderRequest(
    [Range(0, int.MaxValue)] int Version);

public sealed record ChangeStatusRequest(
    [Required] OrderStatus Status,
    [Range(0, int.MaxValue)] int Version);

public sealed record OrderItemRequest(
    [Required] ProductSnapshotRequest Product,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
    string? Notes,
    [Range(0, double.MaxValue)] decimal AddonsValue);

public sealed record CustomerSnapshotRequest(
    [Required] string Id,
    [Required] string Name);

public sealed record RestaurantSnapshotRequest(
    [Required] string Id,
    [Required] string Name);

public sealed record ProductSnapshotRequest(
    [Required] string Id,
    [Required] string Description);
