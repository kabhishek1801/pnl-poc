using PnL.Application.DTO;

namespace PnL.Application.Interfaces;

public interface IFeedPublisher
{
    Task PublishRealtimeAsync(PnLFeedRecord record, CancellationToken cancellationToken = default);

    Task PublishFileUploadedAsync(
        Guid batchId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default);
}
