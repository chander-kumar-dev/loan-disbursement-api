using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Tests.TestSupport;

/// <summary>
/// Shared test values, matching the defaults in appsettings.json.
/// </summary>
public static class TestData
{
    public static readonly LoanLimits Limits = new(minAmount: 1m, maxAmount: 5_000_000m, minTenureMonths: 1, maxTenureMonths: 60);

    /// <summary>A valid IBAN (passes the ISO 13616 mod-97 checksum).</summary>
    public const string ValidIban = "PK36SCBL0000001123456702";
}
