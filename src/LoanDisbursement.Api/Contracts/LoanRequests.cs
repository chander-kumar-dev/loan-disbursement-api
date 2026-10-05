using System.ComponentModel.DataAnnotations;

namespace LoanDisbursement.Api.Contracts;

// [ApiController] checks these attributes and returns 400 Bad Request automatically if they fail.

public record CreateLoanRequest(
    [Required, StringLength(200)] string ApplicantName,
    [Required, StringLength(34)] string AccountNumber,
    [Range(1.0, 5_000_000.0)] decimal Amount,
    [Range(1, 60)] int TenureMonths);

public record RejectLoanRequest(
    [Required, StringLength(500)] string Reason);
