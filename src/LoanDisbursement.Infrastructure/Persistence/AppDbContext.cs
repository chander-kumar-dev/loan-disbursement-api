using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration class in this project, e.g. LoanConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
