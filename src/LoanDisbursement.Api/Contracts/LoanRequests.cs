using System.ComponentModel.DataAnnotations;

namespace LoanDisbursement.Api.Contracts;

// [ApiController] checks these attributes and returns 400 Bad Request automatically if they fail.
// They check shape only (present, not too long). Business limits such as the maximum amount
// live in configuration and are enforced by the Domain, so they are defined exactly once.

public record CreateLoanRequest(
    [Required, StringLength(200)] string ApplicantName,
    // 42 = a 34-character IBAN written in groups of four with spaces; the Domain removes the spaces.
    [Required, StringLength(42)] string AccountNumber,
    decimal Amount,
    int TenureMonths);

public record RejectLoanRequest(
    [Required, StringLength(500)] string Reason);
