using PnL.Domain;

namespace PnL.Application.Interfaces;

public interface INotificationPublisher
{
    Task PublishProcessedAsync(PnLRecord record, CancellationToken cancellationToken = default);
}
