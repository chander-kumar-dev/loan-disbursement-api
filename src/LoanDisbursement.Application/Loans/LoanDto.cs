using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Application.Loans;

/// <summary>
/// What the API returns for a loan. Keeps the domain entity out of the HTTP contract.
/// </summary>
public record LoanDto(
    Guid Id,
    string ApplicantName,
    string AccountNumber,
    decimal Amount,
    int TenureMonths,
    LoanStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? DisbursedAtUtc,
    DateTime? RejectedAtUtc,
    string? RejectionReason)
{
    public static LoanDto FromLoan(Loan loan) => new(
        loan.Id,
        loan.ApplicantName,
        loan.AccountNumber.Value,
        loan.Amount,
        loan.TenureMonths,
        loan.Status,
        loan.CreatedAtUtc,
        loan.ApprovedAtUtc,
        loan.DisbursedAtUtc,
        loan.RejectedAtUtc,
        loan.RejectionReason);
}
