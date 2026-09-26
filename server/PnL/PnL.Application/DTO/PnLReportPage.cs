using PnL.Domain;

namespace PnL.Application.DTO;

// Report page containing a subset of PnL records with pagination information.
public sealed record PnLReportPage(
    IReadOnlyList<PnLRecord> Records,
    int Page,
    int PageSize,
    int TotalCount);