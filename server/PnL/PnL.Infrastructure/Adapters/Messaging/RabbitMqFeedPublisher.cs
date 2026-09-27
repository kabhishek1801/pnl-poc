using System.Text;
using System.Text.Json;
using PnL.Application.DTO;
using PnL.Application.Interfaces;

namespace PnL.Infrastructure.Adapters.Messaging;

public sealed class RabbitMqFeedPublisher(
    RabbitMqConnectionProvider connectionProvider,
    RabbitMqOptions options) : IFeedPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task PublishRealtimeAsync(
        PnLFeedRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        var message = new RealtimeMessage(record.SourceSystem, record.AccountNumber, record.PnLAmount);
        Publish(message, "pnl.realtime");
        return Task.CompletedTask;
    }

    public Task PublishFileUploadedAsync(
        Guid batchId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        var message = new FileUploadedMessage(batchId, fileName, content);
        Publish(message, "pnl.file.uploaded");
        return Task.CompletedTask;
    }

    private void Publish<T>(T message, string routingKey)
    {
        using var channel = connectionProvider.CreateChannel();
        channel.ExchangeDeclare(options.IngestionExchange, "direct", durable: true, autoDelete: false, arguments: null);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: options.IngestionExchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: payload);
    }

    private sealed record RealtimeMessage(string SourceSystem, int AccountNumber, int PnLAmount);

    private sealed record FileUploadedMessage(Guid BatchId, string FileName, byte[] Content);
}
