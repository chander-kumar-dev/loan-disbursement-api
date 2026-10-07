namespace LoanDisbursement.Domain.Loans;

/// <summary>
/// The amount and tenure a loan product allows. The values come from configuration,
/// so the Domain enforces the rules without owning the numbers.
/// </summary>
public sealed record LoanLimits
{
    public decimal MinAmount { get; }
    public decimal MaxAmount { get; }
    public int MinTenureMonths { get; }
    public int MaxTenureMonths { get; }

    public LoanLimits(decimal minAmount, decimal maxAmount, int minTenureMonths, int maxTenureMonths)
    {
        if (minAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minAmount), minAmount, "Minimum amount must be greater than 0.");
        }
        if (maxAmount < minAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAmount), maxAmount, "Maximum amount must be at least the minimum amount.");
        }
        if (minTenureMonths < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minTenureMonths), minTenureMonths, "Minimum tenure must be at least 1 month.");
        }
        if (maxTenureMonths < minTenureMonths)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTenureMonths), maxTenureMonths, "Maximum tenure must be at least the minimum tenure.");
        }

        MinAmount = minAmount;
        MaxAmount = maxAmount;
        MinTenureMonths = minTenureMonths;
        MaxTenureMonths = maxTenureMonths;
    }
}
