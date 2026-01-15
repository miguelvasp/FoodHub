using MongoDB.Bson.Serialization.Attributes;

namespace FoodHub.Orders.Data.Documents;

public sealed class OrderItemDocument
{
    [BsonElement("product")]
    public ProductSnapshotDocument Product { get; set; } = new();

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("addonsValue")]
    public decimal AddonsValue { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("totalItemValue")]
    public decimal TotalItemValue { get; set; }
}
