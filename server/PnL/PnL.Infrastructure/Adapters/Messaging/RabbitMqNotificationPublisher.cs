using System.Text.Json;
using PnL.Application.Interfaces;
using PnL.Domain;

namespace PnL.Infrastructure.Adapters.Messaging;

public sealed class RabbitMqNotificationPublisher(
    RabbitMqConnectionProvider connectionProvider,
    RabbitMqOptions options) : INotificationPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task PublishProcessedAsync(
        PnLRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        using var channel = connectionProvider.CreateChannel();
        channel.ExchangeDeclare(options.NotificationExchange, "fanout", durable: true, autoDelete: false, arguments: null);

        var payload = JsonSerializer.SerializeToUtf8Bytes(record, SerializerOptions);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: options.NotificationExchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: payload);

        return Task.CompletedTask;
    }
}
