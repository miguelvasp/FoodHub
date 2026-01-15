using MongoDB.Bson.Serialization.Attributes;

namespace FoodHub.Orders.Data.Documents;

public sealed class CustomerSnapshotDocument
{
    [BsonElement("id")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string CustomerName { get; set; } = string.Empty;
}
