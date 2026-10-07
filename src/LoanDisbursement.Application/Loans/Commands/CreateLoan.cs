using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Application.Loans.Commands;

public record CreateLoanCommand(string ApplicantName, string AccountNumber, decimal Amount, int TenureMonths);

public class CreateLoanHandler : ICommandHandler<CreateLoanCommand, LoanDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _clock;
    private readonly LoanLimits _limits;

    public CreateLoanHandler(IAppDbContext db, TimeProvider clock, LoanLimits limits)
    {
        _db = db;
        _clock = clock;
        _limits = limits;
    }

    public async Task<LoanDto> HandleAsync(CreateLoanCommand command, CancellationToken cancellationToken)
    {
        var loan = Loan.Create(
            command.ApplicantName,
            command.AccountNumber,
            command.Amount,
            command.TenureMonths,
            _limits,
            _clock.GetUtcNow().UtcDateTime);

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync(cancellationToken);

        return LoanDto.FromLoan(loan);
    }
}
