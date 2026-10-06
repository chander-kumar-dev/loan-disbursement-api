namespace LoanDisbursement.Application.Common;

/// <summary>
/// Thrown when a request is malformed in a way the API layer cannot check on its own,
/// for example a paging cursor that was not issued by this API. Maps to 400 Bad Request.
/// </summary>
public class RequestValidationException : Exception
{
    public RequestValidationException(string message) : base(message)
    {
    }
}
