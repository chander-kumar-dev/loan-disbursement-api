namespace LoanDisbursement.Api.Tests.Infrastructure;

public static class TestRequests
{
    /// <summary>A valid IBAN (passes the ISO 13616 mod-97 checksum).</summary>
    public const string ValidIban = "PK36SCBL0000001123456702";

    public static object NewLoan(
        string? applicantName = "Ayesha Khan",
        string? accountNumber = ValidIban,
        decimal amount = 250_000m,
        int tenureMonths = 12) =>
        new { applicantName, accountNumber, amount, tenureMonths };
}
