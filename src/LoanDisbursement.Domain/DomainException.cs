namespace LoanDisbursement.Domain;

/// <summary>
/// Thrown when an operation would break a business rule,
/// for example disbursing a loan that was never approved.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
