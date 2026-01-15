using MongoDB.Bson.Serialization.Attributes;

namespace FoodHub.Orders.Data.Documents;

public sealed class RestaurantSnapshotDocument
{
    [BsonElement("id")]
    public string RestaurantId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string RestaurantName { get; set; } = string.Empty;
}
