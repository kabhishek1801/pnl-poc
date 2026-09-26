using PnL.Domain;
using PnL.Domain.Enums;

namespace PnL.Application.Interfaces;

public interface IPnLRepository
{
    Task<PnLRecord> UpsertAsync(PnLRecord record, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PnLRecord> Records, int TotalCount)> GetPageAsync(
        PnLStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
