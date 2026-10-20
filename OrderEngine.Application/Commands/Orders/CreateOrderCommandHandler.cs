using System.Text.Json;
using MediatR;
using OrderEngine.Application.DTOs;
using OrderEngine.Application.Events;
using OrderEngine.Domain.Entities;
using OrderEngine.Domain.Interfaces;
using OrderEngine.Domain.Interfaces.Repositories;

namespace OrderEngine.Application.Commands.Orders;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid());

        foreach (var item in request.Items)
            order.AddOrderItem(item.Product, item.Quantity, item.Price);

        await _orderRepository.AddAsync(order, cancellationToken);

        var orderCreated = new OrderCreatedEvent(
            order.Id,
            order.TotalAmount,
            order.Items.Select(i => new OrderItemDto(i.Product, i.Quantity, i.Price)).ToList());

        var outboxMessage = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCreated",
            JsonSerializer.Serialize(orderCreated),
            DateTime.UtcNow);

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        // Pedido e evento são gravados na mesma transação.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
