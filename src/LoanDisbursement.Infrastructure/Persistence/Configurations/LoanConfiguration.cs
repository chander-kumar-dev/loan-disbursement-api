using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanDisbursement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Code-first mapping: this class defines the Loans table. Migrations are generated from it.
/// </summary>
public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    /// <summary>
    /// Shadow property (a column with no matching C# property) used for optimistic concurrency.
    /// AppDbContext gives it a new value on every insert and update.
    /// </summary>
    public const string VersionProperty = "Version";

    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ApplicantName).HasMaxLength(200).IsRequired();
        // Stored as plain text; same column as before, so no migration is needed.
        builder.Property(l => l.AccountNumber)
            .HasConversion(iban => iban.Value, value => Iban.FromTrusted(value))
            .HasMaxLength(34)
            .IsRequired();
        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.RejectionReason).HasMaxLength(500);

        // Store the status as readable text ("Approved") instead of a number
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        // Optimistic concurrency: every UPDATE includes "WHERE Version = <value when loaded>".
        // If another request changed the loan in between, no row matches and EF Core throws
        // DbUpdateConcurrencyException, so two requests can never both disburse the same loan.
        // A SQL Server rowversion would be set by the database automatically, but SQLite (used by the tests)
        // has no equivalent; an app-managed Guid behaves the same on both.
        builder.Property<Guid>(VersionProperty).IsConcurrencyToken();

        // Support keyset pagination (newest first, Id as tie-breaker), with and without a status filter.
        // SQL Server can read these ascending indexes backwards, so no descending index is needed.
        builder.HasIndex(l => new { l.Status, l.CreatedAtUtc, l.Id });
        builder.HasIndex(l => new { l.CreatedAtUtc, l.Id });
    }
}
