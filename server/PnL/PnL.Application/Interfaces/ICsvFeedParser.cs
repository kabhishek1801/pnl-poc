using PnL.Application.Features.Ingestion;

using PnL.Application.DTO;

namespace PnL.Application.Interfaces;

/// <summary>
/// Parses CSV feeds into PnL feed records.
/// </summary>
public interface ICsvFeedParser
{
    IAsyncEnumerable<PnLFeedRecord> ParseAsync(
        Stream csvStream,
        CancellationToken cancellationToken = default);
}
