using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Application.Loans.Queries;

/// <param name="Status">Optional filter. Null returns loans in every status.</param>
public record GetLoansQuery(LoanStatus? Status);

public class GetLoansHandler : IQueryHandler<GetLoansQuery, IReadOnlyList<LoanDto>>
{
    private readonly IAppDbContext _db;

    public GetLoansHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LoanDto>> HandleAsync(GetLoansQuery query, CancellationToken cancellationToken)
    {
        var loans = _db.Loans.AsNoTracking();

        if (query.Status is not null)
        {
            loans = loans.Where(l => l.Status == query.Status);
        }

        var result = await loans
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return result.Select(LoanDto.FromLoan).ToList();
    }
}
