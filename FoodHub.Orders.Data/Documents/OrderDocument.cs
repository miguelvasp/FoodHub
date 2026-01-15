using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FoodHub.Orders.Data.Documents;

public sealed class OrderDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("code")]
    public string OrderCode { get; set; } = string.Empty;

    [BsonElement("orderedAt")]
    public DateTime OrderedAt { get; set; }

    [BsonElement("customer")]
    public CustomerSnapshotDocument Customer { get; set; } = new();

    [BsonElement("restaurant")]
    public RestaurantSnapshotDocument Restaurant { get; set; } = new();

    [BsonElement("items")]
    public List<OrderItemDocument> Items { get; set; } = [];

    [BsonElement("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [BsonElement("coupon")]
    public CouponDocument? Coupon { get; set; }

    [BsonElement("discountValue")]
    public decimal DiscountValue { get; set; }

    [BsonElement("totalValue")]
    public decimal TotalValue { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public Domain.OrderStatus Status { get; set; }

    [BsonElement("orderType")]
    [BsonRepresentation(BsonType.String)]
    public Domain.OrderType OrderType { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }
}
