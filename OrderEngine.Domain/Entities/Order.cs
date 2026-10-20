using OrderEngine.Domain.Enums;

namespace OrderEngine.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = new();

    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(item => item.Subtotal);

    private Order()
    {
    }

    public Order(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Order id cannot be empty.", nameof(id));

        Id = id;
        Status = OrderStatus.Pending;
    }

    public void AddOrderItem(string product, int quantity, decimal price)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Items can only be added while the order is pending.");

        _items.Add(new OrderItem(product, quantity, price));
    }

    public void MarkAsProcessing() =>
        Transition(OrderStatus.Processing, OrderStatus.Pending);

    public void MarkAsCompleted() =>
        Transition(OrderStatus.Completed, OrderStatus.Processing);

    public void MarkAsFailed() =>
        Transition(OrderStatus.Failed, OrderStatus.Pending, OrderStatus.Processing);

    private void Transition(OrderStatus target, params OrderStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
            throw new InvalidOperationException($"Order cannot move from {Status} to {target}.");

        Status = target;
    }
}
