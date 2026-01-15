using MongoDB.Bson.Serialization.Attributes;

namespace FoodHub.Orders.Data.Documents;

public sealed class ProductSnapshotDocument
{
    [BsonElement("id")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("description")]
    public string ProductDescription { get; set; } = string.Empty;
}
