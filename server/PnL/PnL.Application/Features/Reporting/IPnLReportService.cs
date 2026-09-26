using PnL.Application.DTO;

namespace PnL.Application.Features.Reporting;

public interface IPnLReportService
{
    Task<PnLReportPage> GetValidatedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PnLReportPage> GetExcludedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

}
