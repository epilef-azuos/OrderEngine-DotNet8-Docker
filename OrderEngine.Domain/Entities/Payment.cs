using OrderEngine.Domain.Enums;

namespace OrderEngine.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; }

    private Payment()
    {
        IdempotencyKey = null!;
    }

    public Payment(Guid id, Guid orderId, string idempotencyKey)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Payment id cannot be empty.", nameof(id));

        if (orderId == Guid.Empty)
            throw new ArgumentException("Order id cannot be empty.", nameof(orderId));

        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        Id = id;
        OrderId = orderId;
        IdempotencyKey = idempotencyKey;
        Status = PaymentStatus.Pending;
    }

    public void Approve() => Transition(PaymentStatus.Approved);

    public void Reject() => Transition(PaymentStatus.Rejected);

    private void Transition(PaymentStatus target)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Payment cannot move from {Status} to {target}.");

        Status = target;
    }
}
