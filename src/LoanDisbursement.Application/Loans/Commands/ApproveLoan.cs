using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Application.Loans.Commands;

public record ApproveLoanCommand(Guid LoanId);

public class ApproveLoanHandler : ICommandHandler<ApproveLoanCommand, LoanDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _clock;

    public ApproveLoanHandler(IAppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LoanDto> HandleAsync(ApproveLoanCommand command, CancellationToken cancellationToken)
    {
        var loan = await _db.Loans.FindAsync([command.LoanId], cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), command.LoanId);

        loan.Approve(_clock.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);

        return LoanDto.FromLoan(loan);
    }
}
