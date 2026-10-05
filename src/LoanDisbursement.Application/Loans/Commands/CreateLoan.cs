using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Domain.Loans;

namespace LoanDisbursement.Application.Loans.Commands;

public record CreateLoanCommand(string ApplicantName, string AccountNumber, decimal Amount, int TenureMonths);

public class CreateLoanHandler : ICommandHandler<CreateLoanCommand, LoanDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _clock;

    public CreateLoanHandler(IAppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LoanDto> HandleAsync(CreateLoanCommand command, CancellationToken cancellationToken)
    {
        var loan = Loan.Create(
            command.ApplicantName,
            command.AccountNumber,
            command.Amount,
            command.TenureMonths,
            _clock.GetUtcNow().UtcDateTime);

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync(cancellationToken);

        return LoanDto.FromLoan(loan);
    }
}
