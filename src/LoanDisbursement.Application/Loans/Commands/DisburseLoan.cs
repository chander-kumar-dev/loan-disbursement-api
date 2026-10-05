using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Application.Loans.Commands;

public record DisburseLoanCommand(Guid LoanId);

public class DisburseLoanHandler : ICommandHandler<DisburseLoanCommand, LoanDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _clock;

    public DisburseLoanHandler(IAppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LoanDto> HandleAsync(DisburseLoanCommand command, CancellationToken cancellationToken)
    {
        var loan = await _db.Loans.FindAsync([command.LoanId], cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), command.LoanId);

        // Next step of the project: call the core banking system here, with retries and an idempotency key.
        loan.Disburse(_clock.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(cancellationToken);

        return LoanDto.FromLoan(loan);
    }
}
