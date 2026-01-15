using FoodHub.Orders.Data.Documents;
using FoodHub.Orders.Data.Repositories;
using FoodHub.Orders.Domain.Exceptions;
using FoodHub.Orders.Tests.Builders;
using FluentAssertions;
using MongoDB.Driver;
using Moq;

namespace FoodHub.Orders.Tests.Data;

public class OrderRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowConcurrencyConflict_WhenVersionMismatch()
    {
        var order = new OrderBuilder().Build();

        var collectionMock = new Mock<IMongoCollection<OrderDocument>>();
        var indexManagerMock = new Mock<IMongoIndexManager<OrderDocument>>();
        collectionMock.SetupGet(c => c.Indexes).Returns(indexManagerMock.Object);
        indexManagerMock
            .Setup(m => m.CreateOne(
                It.IsAny<CreateIndexModel<OrderDocument>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns("ux_orders_code");

        collectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<OrderDocument>>(),
                It.IsAny<OrderDocument>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));

        collectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<OrderDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var databaseMock = new Mock<IMongoDatabase>();
        databaseMock
            .Setup(d => d.GetCollection<OrderDocument>(
                It.IsAny<string>(),
                It.IsAny<MongoCollectionSettings>()))
            .Returns(collectionMock.Object);

        var repository = new OrderRepository(databaseMock.Object);

        var action = async () => await repository.UpdateAsync(order);

        await action.Should().ThrowAsync<ConcurrencyConflictException>()
            .WithMessage("Order update conflict.");
    }
}
