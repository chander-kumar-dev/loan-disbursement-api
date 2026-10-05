using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Application.Abstractions;

/// <summary>
/// The database as seen by the Application layer.
/// The real implementation lives in Infrastructure, so handlers do not depend on SQL Server.
/// </summary>
public interface IAppDbContext
{
    DbSet<Loan> Loans { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
