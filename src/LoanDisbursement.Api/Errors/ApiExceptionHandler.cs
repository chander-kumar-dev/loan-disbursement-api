using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LoanDisbursement.Api.Errors;

/// <summary>
/// Turns known exceptions into clear HTTP responses (RFC 7807 problem details).
/// Anything else falls through to the default 500 response.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public ApiExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title)? mapped = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            _ => null
        };

        if (mapped is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = mapped.Value.status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapped.Value.status,
                Title = mapped.Value.title,
                Detail = exception.Message
            }
        });
    }
}
