namespace PnL.Domain.Rules;

public static class ZeroAmountExclusionRule
{
    public const string ExclusionReason = "ZeroPnLAmount";

    public static bool IsExcluded(int pnlAmount) => pnlAmount <= 0;
}
