namespace OrderEngine.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedOn { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage()
    {
        Type = null!;
        Content = null!;
    }

    public OutboxMessage(Guid id, string type, string content, DateTime occurredOn)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Outbox message id cannot be empty.", nameof(id));

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (occurredOn.Kind != DateTimeKind.Utc)
            throw new ArgumentException("OccurredOn must be a UTC date.", nameof(occurredOn));

        Id = id;
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
    }

    public void MarkAsProcessed()
    {
        ProcessedOn = DateTime.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        Error = error;
    }
}
