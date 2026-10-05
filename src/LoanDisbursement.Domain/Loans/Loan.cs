namespace LoanDisbursement.Domain.Loans;

/// <summary>
/// A loan and its lifecycle: Pending → Approved → Disbursed, or Pending → Rejected.
/// All state changes go through methods, so an invalid transition is impossible.
/// </summary>
public class Loan
{
    public const decimal MaxAmount = 5_000_000m;
    public const int MaxTenureMonths = 60;

    public Guid Id { get; private set; }
    public string ApplicantName { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public int TenureMonths { get; private set; }
    public LoanStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? DisbursedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    // Required by EF Core to create objects when reading from the database
    private Loan()
    {
    }

    public static Loan Create(string applicantName, string accountNumber, decimal amount, int tenureMonths, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(applicantName))
        {
            throw new DomainException("Applicant name is required.");
        }
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new DomainException("Account number is required.");
        }
        if (amount <= 0 || amount > MaxAmount)
        {
            throw new DomainException($"Amount must be greater than 0 and at most {MaxAmount}.");
        }
        if (tenureMonths < 1 || tenureMonths > MaxTenureMonths)
        {
            throw new DomainException($"Tenure must be between 1 and {MaxTenureMonths} months.");
        }

        return new Loan
        {
            Id = Guid.NewGuid(),
            ApplicantName = applicantName.Trim(),
            AccountNumber = accountNumber.Trim(),
            Amount = amount,
            TenureMonths = tenureMonths,
            Status = LoanStatus.Pending,
            CreatedAtUtc = nowUtc
        };
    }

    public void Approve(DateTime nowUtc)
    {
        EnsureStatus(LoanStatus.Pending, "approve");
        Status = LoanStatus.Approved;
        ApprovedAtUtc = nowUtc;
    }

    public void Reject(string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A rejection reason is required.");
        }

        EnsureStatus(LoanStatus.Pending, "reject");
        Status = LoanStatus.Rejected;
        RejectionReason = reason.Trim();
        RejectedAtUtc = nowUtc;
    }

    public void Disburse(DateTime nowUtc)
    {
        EnsureStatus(LoanStatus.Approved, "disburse");
        Status = LoanStatus.Disbursed;
        DisbursedAtUtc = nowUtc;
    }

    private void EnsureStatus(LoanStatus required, string action)
    {
        if (Status != required)
        {
            throw new DomainException($"Cannot {action} a loan with status {Status}. Required status: {required}.");
        }
    }
}
