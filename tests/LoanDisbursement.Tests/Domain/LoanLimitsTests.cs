using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Tests.Domain;

public class LoanLimitsTests
{
    [Theory]
    [InlineData(0, 100, 1, 12)]   // minimum amount must be positive
    [InlineData(200, 100, 1, 12)] // maximum below minimum
    [InlineData(1, 100, 0, 12)]   // minimum tenure below one month
    [InlineData(1, 100, 12, 6)]   // maximum tenure below minimum
    public void InvalidLimits_Throw(int minAmount, int maxAmount, int minTenure, int maxTenure)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LoanLimits(minAmount, maxAmount, minTenure, maxTenure));
    }

    [Fact]
    public void EqualMinimumAndMaximum_AreAllowed()
    {
        var limits = new LoanLimits(1_000m, 1_000m, 12, 12);

        Assert.Equal(1_000m, limits.MaxAmount);
        Assert.Equal(12, limits.MaxTenureMonths);
    }
}
