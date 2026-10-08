using LoanDisbursement.Domain;
using LoanDisbursement.Domain.Loans;
using LoanDisbursement.Tests.TestSupport;

namespace LoanDisbursement.Tests.Domain;

public class LoanTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    private static Loan NewLoan() => Loan.Create("Ayesha Khan", TestData.ValidIban, 250_000m, 12, TestData.Limits, Now);

    [Fact]
    public void Create_WithValidData_StartsAsPending()
    {
        var loan = NewLoan();

        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal(250_000m, loan.Amount);
        Assert.Equal(Now, loan.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, loan.Id);
    }

    [Fact]
    public void Create_TrimsApplicantName()
    {
        var loan = Loan.Create("  Ayesha Khan  ", TestData.ValidIban, 1_000m, 6, TestData.Limits, Now);

        Assert.Equal("Ayesha Khan", loan.ApplicantName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5_000_001)]
    public void Create_WithInvalidAmount_ThrowsValidationError(int amount)
    {
        Assert.Throws<DomainValidationException>(() => Loan.Create("Ayesha Khan", TestData.ValidIban, amount, 12, TestData.Limits, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Create_WithInvalidTenure_ThrowsValidationError(int tenureMonths)
    {
        Assert.Throws<DomainValidationException>(() => Loan.Create("Ayesha Khan", TestData.ValidIban, 1_000m, tenureMonths, TestData.Limits, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutApplicantName_ThrowsValidationError(string name)
    {
        Assert.Throws<DomainValidationException>(() => Loan.Create(name, TestData.ValidIban, 1_000m, 12, TestData.Limits, Now));
    }

    [Fact]
    public void Create_WithInvalidIban_ThrowsValidationError()
    {
        Assert.Throws<DomainValidationException>(() => Loan.Create("Ayesha Khan", "PK36SCBL0000001123456703", 1_000m, 12, TestData.Limits, Now));
    }

    [Fact]
    public void Create_StoresNormalisedIban()
    {
        var loan = Loan.Create("Ayesha Khan", "pk36 scbl 0000 0011 2345 6702", 1_000m, 12, TestData.Limits, Now);

        Assert.Equal(TestData.ValidIban, loan.AccountNumber.Value);
    }

    [Fact]
    public void Create_UsesTheLimitsItIsGiven_NotHardCodedValues()
    {
        var smallProduct = new LoanLimits(minAmount: 500m, maxAmount: 2_000m, minTenureMonths: 3, maxTenureMonths: 6);

        Assert.Throws<DomainValidationException>(() => Loan.Create("Ayesha Khan", TestData.ValidIban, 2_001m, 6, smallProduct, Now));
        Assert.Throws<DomainValidationException>(() => Loan.Create("Ayesha Khan", TestData.ValidIban, 499m, 6, smallProduct, Now));
        Assert.Throws<DomainValidationException>(() => Loan.Create("Ayesha Khan", TestData.ValidIban, 1_000m, 7, smallProduct, Now));

        var loan = Loan.Create("Ayesha Khan", TestData.ValidIban, 2_000m, 3, smallProduct, Now);
        Assert.Equal(2_000m, loan.Amount);
    }

    [Fact]
    public void Approve_PendingLoan_SetsApproved()
    {
        var loan = NewLoan();

        loan.Approve(Now);

        Assert.Equal(LoanStatus.Approved, loan.Status);
        Assert.Equal(Now, loan.ApprovedAtUtc);
    }

    [Fact]
    public void Approve_Twice_Throws()
    {
        var loan = NewLoan();
        loan.Approve(Now);

        Assert.Throws<DomainException>(() => loan.Approve(Now));
    }

    [Fact]
    public void Disburse_PendingLoan_Throws()
    {
        var loan = NewLoan();

        Assert.Throws<DomainException>(() => loan.Disburse(Now));
        Assert.Equal(LoanStatus.Pending, loan.Status);
    }

    [Fact]
    public void Disburse_ApprovedLoan_SetsDisbursed()
    {
        var loan = NewLoan();
        loan.Approve(Now);

        loan.Disburse(Now);

        Assert.Equal(LoanStatus.Disbursed, loan.Status);
        Assert.Equal(Now, loan.DisbursedAtUtc);
    }

    [Fact]
    public void Disburse_Twice_Throws_SoMoneyIsNeverPaidOutTwice()
    {
        var loan = NewLoan();
        loan.Approve(Now);
        loan.Disburse(Now);

        Assert.Throws<DomainException>(() => loan.Disburse(Now));
    }

    [Fact]
    public void Reject_PendingLoan_StoresReason()
    {
        var loan = NewLoan();

        loan.Reject("  Insufficient income ", Now);

        Assert.Equal(LoanStatus.Rejected, loan.Status);
        Assert.Equal("Insufficient income", loan.RejectionReason);
    }

    [Fact]
    public void Reject_ApprovedLoan_Throws()
    {
        var loan = NewLoan();
        loan.Approve(Now);

        Assert.Throws<DomainException>(() => loan.Reject("Changed our mind", Now));
    }

    [Fact]
    public void Reject_WithoutReason_ThrowsValidationError()
    {
        var loan = NewLoan();

        Assert.Throws<DomainValidationException>(() => loan.Reject(" ", Now));
    }
}
