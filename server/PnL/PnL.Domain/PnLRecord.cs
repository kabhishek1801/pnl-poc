using PnL.Domain.Enums;
using PnL.Domain.Rules;

namespace PnL.Domain;

public class PnLRecord
{
    public PnLRecord(
        int accountNumber,
        string sourceSystem,
        int pnlAmount,
        FeedSource feedSource,
        Guid? batchId,
        DateTime capturedAtUtc)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        Update(sourceSystem, pnlAmount, feedSource, batchId, capturedAtUtc);
    }

    public Guid Id { get; }

    public int AccountNumber { get; }

    public string SourceSystem { get; private set; } = string.Empty;

    public int PnLAmount { get; private set; }

    public PnLStatus Status { get; private set; }

    public FeedSource FeedSource { get; private set; }

    public string? ExclusionReason { get; private set; }

    public Guid? BatchId { get; private set; }

    public DateTime CapturedAtUtc { get; private set; }


// Updates the PnL record with new values and applies the exclusion rules.
    public void Update(
        string sourceSystem,
        int pnlAmount,
        FeedSource feedSource,
        Guid? batchId,
        DateTime capturedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(sourceSystem))
        {
            throw new ArgumentException("Source system is required.", nameof(sourceSystem));
        }

        SourceSystem = sourceSystem;
        PnLAmount = pnlAmount;
        FeedSource = feedSource;
        BatchId = batchId;
        CapturedAtUtc = capturedAtUtc;
        ApplyStatus();
    }
	
	   private void ApplyStatus()
    {
        if (ZeroAmountExclusionRule.IsExcluded(PnLAmount))
        {
            Status = PnLStatus.Excluded;
            ExclusionReason = ZeroAmountExclusionRule.ExclusionReason;
            return;
        }

        Status = PnLStatus.Valid;
        ExclusionReason = null;
    }
}
