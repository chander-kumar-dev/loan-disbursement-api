using LoanDisbursement.Domain;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Tests.Domain;

public class LoanTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    private static Loan NewLoan() => Loan.Create("Ayesha Khan", "PK36SCBL0000001123456702", 250_000m, 12, Now);

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
        var loan = Loan.Create("  Ayesha Khan  ", "PK36", 1_000m, 6, Now);

        Assert.Equal("Ayesha Khan", loan.ApplicantName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5_000_001)]
    public void Create_WithInvalidAmount_Throws(int amount)
    {
        Assert.Throws<DomainException>(() => Loan.Create("Ayesha Khan", "PK36", amount, 12, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Create_WithInvalidTenure_Throws(int tenureMonths)
    {
        Assert.Throws<DomainException>(() => Loan.Create("Ayesha Khan", "PK36", 1_000m, tenureMonths, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutApplicantName_Throws(string name)
    {
        Assert.Throws<DomainException>(() => Loan.Create(name, "PK36", 1_000m, 12, Now));
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
    public void Reject_WithoutReason_Throws()
    {
        var loan = NewLoan();

        Assert.Throws<DomainException>(() => loan.Reject(" ", Now));
    }
}
