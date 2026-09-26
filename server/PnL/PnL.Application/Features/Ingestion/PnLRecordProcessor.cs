using PnL.Application.DTO;
using PnL.Application.Interfaces;
using PnL.Domain;
using PnL.Domain.Enums;

namespace PnL.Application.Features.Ingestion;

public sealed class PnLRecordProcessor(IPnLRepository repository, INotificationPublisher notificationPublisher)
    : IPnLRecordProcessor
{
    public async Task<PnLRecord> ProcessAsync(
        PnLFeedRecord feedRecord,
        FeedSource feedSource,
        Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feedRecord);

        var record = new PnLRecord(
            feedRecord.AccountNumber,
            feedRecord.SourceSystem,
            feedRecord.PnLAmount,
            feedSource,
            batchId,
            DateTime.UtcNow);

        var savedRecord = await repository.UpsertAsync(record, cancellationToken);
        await notificationPublisher.PublishProcessedAsync(savedRecord, cancellationToken);

        return savedRecord;
    }
}
