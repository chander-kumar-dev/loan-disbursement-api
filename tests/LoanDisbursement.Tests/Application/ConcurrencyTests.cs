using LoanDisbursement.Application.Loans.Commands;
using LoanDisbursement.Domain.Loans;
using LoanDisbursement.Infrastructure.Persistence.Configurations;
using LoanDisbursement.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Tests.Application;

/// <summary>
/// Reproduces the double-disbursement race deterministically, without threads:
/// two requests load the same approved loan, then both try to save.
/// </summary>
public class ConcurrencyTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly CancellationToken _ct = CancellationToken.None;

    public void Dispose() => _database.Dispose();

    private async Task<Guid> CreateApprovedLoanAsync()
    {
        Guid id;
        using (var db = _database.CreateContext())
        {
            var loan = await new CreateLoanHandler(db, _clock, TestData.Limits)
                .HandleAsync(new CreateLoanCommand("Ayesha Khan", TestData.ValidIban, 250_000m, 12), _ct);
            id = loan.Id;
        }

        using (var db = _database.CreateContext())
        {
            await new ApproveLoanHandler(db, _clock).HandleAsync(new ApproveLoanCommand(id), _ct);
        }

        return id;
    }

    [Fact]
    public async Task TwoRequestsDisbursingTheSameLoan_OnlyTheFirstSaveSucceeds()
    {
        var id = await CreateApprovedLoanAsync();

        // Request A loads the loan: it sees Approved.
        using var requestA = _database.CreateContext();
        var loanSeenByA = await requestA.Loans.SingleAsync(l => l.Id == id, _ct);

        // Request B runs completely in between and disburses the loan.
        using (var requestB = _database.CreateContext())
        {
            await new DisburseLoanHandler(requestB, _clock).HandleAsync(new DisburseLoanCommand(id), _ct);
        }

        // Request A still holds the stale "Approved" loan, so the domain check passes...
        loanSeenByA.Disburse(_clock.GetUtcNow().UtcDateTime);

        // ...but the save is rejected, because the Version it loaded is no longer current.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => requestA.SaveChangesAsync(_ct));
    }

    [Fact]
    public async Task EverySave_GivesTheLoanANewVersion()
    {
        var id = await CreateApprovedLoanAsync();

        using var db = _database.CreateContext();
        var loan = await db.Loans.SingleAsync(l => l.Id == id, _ct);
        var versionBefore = (Guid)db.Entry(loan).Property(LoanConfiguration.VersionProperty).CurrentValue!;

        loan.Disburse(_clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(_ct);
        var versionAfter = (Guid)db.Entry(loan).Property(LoanConfiguration.VersionProperty).CurrentValue!;

        Assert.NotEqual(Guid.Empty, versionBefore);
        Assert.NotEqual(versionBefore, versionAfter);
        Assert.Equal(LoanStatus.Disbursed, loan.Status);
    }
}
