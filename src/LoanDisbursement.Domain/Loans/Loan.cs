namespace LoanDisbursement.Domain.Loans;

/// <summary>
/// A loan and its lifecycle: Pending → Approved → Disbursed, or Pending → Rejected.
/// All state changes go through methods, so an invalid transition is impossible.
/// </summary>
public class Loan
{
    public Guid Id { get; private set; }
    public string ApplicantName { get; private set; } = string.Empty;
    public Iban AccountNumber { get; private set; } = null!;
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

    public static Loan Create(
        string applicantName,
        string accountNumber,
        decimal amount,
        int tenureMonths,
        LoanLimits limits,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (string.IsNullOrWhiteSpace(applicantName))
        {
            throw new DomainValidationException("Applicant name is required.");
        }

        var iban = Iban.Create(accountNumber);

        if (amount < limits.MinAmount || amount > limits.MaxAmount)
        {
            throw new DomainValidationException($"Amount must be between {limits.MinAmount} and {limits.MaxAmount}.");
        }
        if (tenureMonths < limits.MinTenureMonths || tenureMonths > limits.MaxTenureMonths)
        {
            throw new DomainValidationException($"Tenure must be between {limits.MinTenureMonths} and {limits.MaxTenureMonths} months.");
        }

        return new Loan
        {
            Id = Guid.NewGuid(),
            ApplicantName = applicantName.Trim(),
            AccountNumber = iban,
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
            throw new DomainValidationException("A rejection reason is required.");
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
