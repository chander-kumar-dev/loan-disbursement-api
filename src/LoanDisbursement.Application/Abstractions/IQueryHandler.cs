namespace LoanDisbursement.Application.Abstractions;

/// <summary>
/// Handles a query: an operation that only reads data and never changes it.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
