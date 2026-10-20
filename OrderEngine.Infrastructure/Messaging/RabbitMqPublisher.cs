using System.Text;
using RabbitMQ.Client;

namespace OrderEngine.Infrastructure.Messaging;

public class RabbitMqPublisher
{
    public const string ExchangeName = "order.events";

    private readonly RabbitMqConnectionProvider _connectionProvider;

    public RabbitMqPublisher(RabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task PublishAsync(string type, string content, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            Type = type,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(content),
            cancellationToken: cancellationToken);
    }
}
