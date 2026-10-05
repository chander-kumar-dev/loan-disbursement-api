using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Application.Loans.Queries;

public record GetLoanByIdQuery(Guid LoanId);

public class GetLoanByIdHandler : IQueryHandler<GetLoanByIdQuery, LoanDto>
{
    private readonly IAppDbContext _db;

    public GetLoanByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<LoanDto> HandleAsync(GetLoanByIdQuery query, CancellationToken cancellationToken)
    {
        // AsNoTracking: read-only, so EF Core does not need to watch the entity for changes
        var loan = await _db.Loans
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == query.LoanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), query.LoanId);

        return LoanDto.FromLoan(loan);
    }
}
