using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Application.Loans.Commands;

public record RejectLoanCommand(Guid LoanId, string Reason);

public class RejectLoanHandler : ICommandHandler<RejectLoanCommand, LoanDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _clock;

    public RejectLoanHandler(IAppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LoanDto> HandleAsync(RejectLoanCommand command, CancellationToken cancellationToken)
    {
        var loan = await _db.Loans.FindAsync([command.LoanId], cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), command.LoanId);

        loan.Reject(command.Reason, _clock.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);

        return LoanDto.FromLoan(loan);
    }
}
