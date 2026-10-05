namespace LoanDisbursement.Application.Abstractions;

/// <summary>
/// Handles a command: an operation that changes data (create, approve, disburse).
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
