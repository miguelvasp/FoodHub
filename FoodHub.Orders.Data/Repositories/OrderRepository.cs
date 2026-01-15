using FoodHub.Orders.Data.Documents;
using FoodHub.Orders.Data.Mapping;
using FoodHub.Orders.Domain;
using FoodHub.Orders.Domain.Exceptions;
using MongoDB.Driver;

namespace FoodHub.Orders.Data.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly IMongoCollection<OrderDocument> _collection;

    public OrderRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<OrderDocument>("orders");
        CreateIndexes();
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var document = OrderDocumentMapper.FromDomain(order);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return OrderDocumentMapper.ToDomain(document);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(doc => doc.Id == orderId)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : OrderDocumentMapper.ToDomain(document);
    }

    public async Task<Order?> GetByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(doc => doc.OrderCode == orderCode)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : OrderDocumentMapper.ToDomain(document);
    }

    public async Task<IReadOnlyList<Order>> SearchAsync(
        string? orderCode,
        OrderStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<OrderDocument>>();

        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            filters.Add(Builders<OrderDocument>.Filter.Eq(doc => doc.OrderCode, orderCode));
        }

        if (status.HasValue)
        {
            filters.Add(Builders<OrderDocument>.Filter.Eq(doc => doc.Status, status.Value));
        }

        if (from.HasValue)
        {
            filters.Add(Builders<OrderDocument>.Filter.Gte(doc => doc.OrderedAt, from.Value));
        }

        if (to.HasValue)
        {
            filters.Add(Builders<OrderDocument>.Filter.Lte(doc => doc.OrderedAt, to.Value));
        }

        var filter = filters.Count == 0
            ? Builders<OrderDocument>.Filter.Empty
            : Builders<OrderDocument>.Filter.And(filters);

        var documents = await _collection.Find(filter)
            .ToListAsync(cancellationToken);

        return documents.Select(OrderDocumentMapper.ToDomain).ToList();
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var document = OrderDocumentMapper.FromDomain(order);
        document.Version = order.Version + 1;

        var filter = Builders<OrderDocument>.Filter.And(
            Builders<OrderDocument>.Filter.Eq(doc => doc.Id, document.Id),
            Builders<OrderDocument>.Filter.Eq(doc => doc.Version, order.Version));

        var result = await _collection.ReplaceOneAsync(
            filter,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            var existingCount = await _collection.CountDocumentsAsync(
                Builders<OrderDocument>.Filter.Eq(doc => doc.Id, document.Id),
                cancellationToken: cancellationToken);

            if (existingCount == 0)
            {
                throw new NotFoundException("Order not found.");
            }

            throw new ConcurrencyConflictException("Order update conflict.");
        }

        return OrderDocumentMapper.ToDomain(document);
    }

    public async Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(doc => doc.Id == orderId, cancellationToken);
        if (result.DeletedCount == 0)
        {
            throw new NotFoundException("Order not found.");
        }
    }

    public async Task DeleteByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(doc => doc.OrderCode == orderCode, cancellationToken);
        if (result.DeletedCount == 0)
        {
            throw new NotFoundException("Order not found.");
        }
    }

    private void CreateIndexes()
    {
        var keys = Builders<OrderDocument>.IndexKeys.Ascending(doc => doc.OrderCode);
        var options = new CreateIndexOptions { Unique = true, Name = "ux_orders_code" };
        _collection.Indexes.CreateOne(new CreateIndexModel<OrderDocument>(keys, options));
    }
}
