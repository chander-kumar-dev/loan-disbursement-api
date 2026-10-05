using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanDisbursement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Code-first mapping: this class defines the Loans table. Migrations are generated from it.
/// </summary>
public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ApplicantName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.AccountNumber).HasMaxLength(34).IsRequired();
        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.RejectionReason).HasMaxLength(500);

        // Store the status as readable text ("Approved") instead of a number
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        // Speeds up "list loans by status", the most common query
        builder.HasIndex(l => l.Status);
    }
}
