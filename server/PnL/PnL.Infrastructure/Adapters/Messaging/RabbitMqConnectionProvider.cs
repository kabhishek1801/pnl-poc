using RabbitMQ.Client;

namespace PnL.Infrastructure.Adapters.Messaging;

public sealed class RabbitMqConnectionProvider(RabbitMqOptions options) : IDisposable
{
    private readonly IConnection connection = new ConnectionFactory
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost
    }.CreateConnection();

    public IModel CreateChannel() => connection.CreateModel();

    public void Dispose() => connection.Dispose();
}
