namespace PnL.Application.DTO;

public sealed record PnLFeedRecord(
    string SourceSystem,
    int AccountNumber,
    int PnLAmount);