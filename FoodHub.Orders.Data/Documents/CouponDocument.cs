using MongoDB.Bson.Serialization.Attributes;

namespace FoodHub.Orders.Data.Documents;

public sealed class CouponDocument
{
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;
}
