using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        (int status, string title, string detail)? mapped = exception switch
        {
            RequestValidationException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            // Order matters: DomainValidationException derives from DomainException.
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated", exception.Message),
            // EF Core's own message describes affected row counts; give the client something actionable instead.
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent update",
                "The loan was changed by another request at the same time. Reload it and try again."),
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
                Detail = mapped.Value.detail
            }
        });
    }
}
