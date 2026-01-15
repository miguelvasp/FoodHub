using FoodHub.Orders.Domain;

namespace FoodHub.Orders.Data.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> SearchAsync(
        string? orderCode,
        OrderStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task DeleteByCodeAsync(string orderCode, CancellationToken cancellationToken = default);
}
