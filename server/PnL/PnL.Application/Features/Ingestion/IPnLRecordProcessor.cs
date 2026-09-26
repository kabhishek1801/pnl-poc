using PnL.Application.DTO;
using PnL.Domain;
using PnL.Domain.Enums;

namespace PnL.Application.Features.Ingestion;

// Defines the contract for processing PnL feed records into PnL records.
public interface IPnLRecordProcessor
{
    Task<PnLRecord> ProcessAsync(
        PnLFeedRecord feedRecord,
        FeedSource feedSource,
        Guid? batchId = null,
        CancellationToken cancellationToken = default);
}
    