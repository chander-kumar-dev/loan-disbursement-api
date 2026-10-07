namespace LoanDisbursement.Domain;

/// <summary>
/// Thrown when input breaks a business rule, for example an amount above the product limit.
/// The API maps it to 400 Bad Request: the caller must change the request.
/// Its base class, <see cref="DomainException"/>, maps to 409 Conflict: the request is valid,
/// but the loan's current state does not allow it.
/// </summary>
public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
