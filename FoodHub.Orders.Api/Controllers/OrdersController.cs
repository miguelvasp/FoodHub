using FoodHub.Orders.Api.Contracts;
using FoodHub.Orders.Data.Repositories;
using FoodHub.Orders.Domain;
using FoodHub.Orders.Domain.Entities;
using FoodHub.Orders.Domain.Exceptions;
using FoodHub.Orders.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Orders.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderRepository repository, ILogger<OrdersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = Order.Create(
            request.Code,
            request.OrderedAt,
            new CustomerSnapshot(request.Customer.Id, request.Customer.Name),
            new RestaurantSnapshot(request.Restaurant.Id, request.Restaurant.Name),
            request.OrderType,
            request.DeliveryFee,
            request.Items.Select(MapOrderItem),
            string.IsNullOrWhiteSpace(request.CouponCode)
                ? null
                : new Coupon(request.CouponCode));

        var created = await _repository.AddAsync(order, cancellationToken);
        LogDomainEvents(order);

        return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, MapOrderResponse(created));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            throw new DomainValidationException("Invalid order id.");
        }

        var order = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("Order not found.");
        }

        return Ok(MapOrderResponse(order));
    }

    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetByCode(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainValidationException("Order code is required.");
        }

        var order = await _repository.GetByCodeAsync(code, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("Order not found.");
        }

        return Ok(MapOrderResponse(order));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> Search(
        [FromQuery] string? code,
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var orders = await _repository.SearchAsync(code, status, from, to, cancellationToken);
        var response = orders.Select(MapOrderResponse).ToList();
        return Ok(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Update(
        string id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            throw new DomainValidationException("Invalid order id.");
        }

        var existing = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Order not found.");
        }

        var updated = Order.Rehydrate(
            existing.OrderId,
            existing.OrderCode,
            request.OrderedAt,
            new CustomerSnapshot(request.Customer.Id, request.Customer.Name),
            new RestaurantSnapshot(request.Restaurant.Id, request.Restaurant.Name),
            request.OrderType,
            request.DeliveryFee,
            request.Items.Select(MapOrderItem),
            existing.Status,
            request.Version,
            string.IsNullOrWhiteSpace(request.CouponCode)
                ? null
                : new Coupon(request.CouponCode));

        updated.MarkUpdated();
        var saved = await _repository.UpdateAsync(updated, cancellationToken);
        LogDomainEvents(updated);

        return Ok(MapOrderResponse(saved));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            throw new DomainValidationException("Invalid order id.");
        }

        await _repository.DeleteAsync(orderId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Cancel(
        string id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            throw new DomainValidationException("Invalid order id.");
        }

        var existing = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Order not found.");
        }

        var order = Order.Rehydrate(
            existing.OrderId,
            existing.OrderCode,
            existing.CreatedAt,
            existing.Customer,
            existing.Restaurant,
            existing.Type,
            existing.DeliveryFee,
            existing.Items.ToList(),
            existing.Status,
            request.Version,
            existing.Coupon);

        order.Cancel();

        var saved = await _repository.UpdateAsync(order, cancellationToken);
        LogDomainEvents(order);
        return Ok(MapOrderResponse(saved));
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> ChangeStatus(
        string id,
        [FromBody] ChangeStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            throw new DomainValidationException("Invalid order id.");
        }

        var existing = await _repository.GetByIdAsync(orderId, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException("Order not found.");
        }

        var order = Order.Rehydrate(
            existing.OrderId,
            existing.OrderCode,
            existing.CreatedAt,
            existing.Customer,
            existing.Restaurant,
            existing.Type,
            existing.DeliveryFee,
            existing.Items.ToList(),
            existing.Status,
            request.Version,
            existing.Coupon);

        order.ChangeStatus(request.Status);

        var saved = await _repository.UpdateAsync(order, cancellationToken);
        LogDomainEvents(order);
        return Ok(MapOrderResponse(saved));
    }

    [HttpDelete("by-code/{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByCode(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainValidationException("Order code is required.");
        }

        await _repository.DeleteByCodeAsync(code, cancellationToken);
        return NoContent();
    }

    private static OrderItem MapOrderItem(OrderItemRequest item)
    {
        return new OrderItem(
            new ProductSnapshot(item.Product.Id, item.Product.Description),
            item.Quantity,
            item.UnitPrice,
            item.AddonsValue,
            item.Notes);
    }

    private static OrderResponse MapOrderResponse(Order order)
    {
        return new OrderResponse(
            order.OrderId.ToString(),
            order.OrderCode,
            order.CreatedAt,
            new CustomerSnapshotResponse(order.Customer.CustomerId, order.Customer.CustomerName),
            new RestaurantSnapshotResponse(order.Restaurant.RestaurantId, order.Restaurant.RestaurantName),
            order.Items.Select(item => new OrderItemResponse(
                item.Product.ProductId,
                new ProductSnapshotResponse(item.Product.ProductId, item.Product.ProductDescription),
                item.Quantity,
                item.UnitPrice,
                item.Notes,
                item.AddonsValue,
                item.TotalItemValue)).ToList(),
            order.DeliveryFee,
            order.Coupon?.Code,
            order.DiscountValue,
            order.OrderTotal,
            order.Status,
            order.Type,
            order.Version);
    }

    private void LogDomainEvents(Order order)
    {
        var events = order.DequeueDomainEvents();
        foreach (var domainEvent in events)
        {
            _logger.LogInformation("Domain event: {EventType} {@Event}", domainEvent.GetType().Name, domainEvent);
        }
    }
}
