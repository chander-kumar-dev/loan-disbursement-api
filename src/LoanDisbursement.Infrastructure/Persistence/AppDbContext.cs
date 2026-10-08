using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Domain.Loans;
using LoanDisbursement.Infrastructure.Persistence.Configurations;
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

    // SaveChangesAsync(CancellationToken) and SaveChanges() both end up in these two overloads.

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampConcurrencyVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampConcurrencyVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Gives every new or changed loan a new Version. Done here, in one place, so no handler can forget it.
    /// EF Core still compares against the Version that was loaded, which is what detects the conflict.
    /// </summary>
    private void StampConcurrencyVersions()
    {
        // Entries() runs change detection first, so loans changed through domain methods show as Modified.
        foreach (var entry in ChangeTracker.Entries<Loan>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(LoanConfiguration.VersionProperty).CurrentValue = Guid.NewGuid();
            }
        }
    }
}
