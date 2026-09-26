using PnL.Application.DTO;
using PnL.Application.Interfaces;
using PnL.Domain.Enums;

namespace PnL.Application.Features.Reporting;

public sealed class PnLReportService(IPnLRepository repository) : IPnLReportService
{
    public Task<PnLReportPage> GetValidatedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(PnLStatus.Valid, page, pageSize, cancellationToken);

    public Task<PnLReportPage> GetExcludedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(PnLStatus.Excluded, page, pageSize, cancellationToken);

    private async Task<PnLReportPage> GetPageAsync(
        PnLStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        }

        var (records, totalCount) = await repository.GetPageAsync(
            status,
            page,
            pageSize,
            cancellationToken);

        return new PnLReportPage(records, page, pageSize, totalCount);
    }
}
