using MediatR;
using OrderEngine.Domain.Entities;
using OrderEngine.Domain.Interfaces.Repositories;

namespace OrderEngine.Application.Commands.Orders;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid());

        foreach (var item in request.Items)
            order.AddOrderItem(item.Product, item.Quantity, item.Price);

        await _orderRepository.AddAsync(order, cancellationToken);

        return order.Id;
    }
}
