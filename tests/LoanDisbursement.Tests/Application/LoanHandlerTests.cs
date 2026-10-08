using LoanDisbursement.Application.Common;
using LoanDisbursement.Application.Loans.Commands;
using LoanDisbursement.Application.Loans.Queries;
using LoanDisbursement.Domain;
using LoanDisbursement.Domain.Loans;
using LoanDisbursement.Tests.TestSupport;

namespace LoanDisbursement.Tests.Application;

public class LoanHandlerTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly CancellationToken _ct = CancellationToken.None;

    public void Dispose() => _database.Dispose();

    private async Task<Guid> CreateLoanAsync(string applicant = "Ayesha Khan")
    {
        using var db = _database.CreateContext();
        var handler = new CreateLoanHandler(db, _clock, TestData.Limits);
        var loan = await handler.HandleAsync(new CreateLoanCommand(applicant, TestData.ValidIban, 250_000m, 12), _ct);
        return loan.Id;
    }

    [Fact]
    public async Task CreateLoan_SavesPendingLoanToDatabase()
    {
        var id = await CreateLoanAsync();

        using var db = _database.CreateContext();
        var loan = await new GetLoanByIdHandler(db).HandleAsync(new GetLoanByIdQuery(id), _ct);

        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal("Ayesha Khan", loan.ApplicantName);
        Assert.Equal(250_000m, loan.Amount);
        Assert.Equal(TestData.ValidIban, loan.AccountNumber);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, loan.CreatedAtUtc);
    }

    [Fact]
    public async Task ApproveThenDisburse_SavesStatusAndTimestamps()
    {
        var id = await CreateLoanAsync();

        _clock.Advance(TimeSpan.FromHours(1));
        using (var db = _database.CreateContext())
        {
            await new ApproveLoanHandler(db, _clock).HandleAsync(new ApproveLoanCommand(id), _ct);
        }
        var approvedAt = _clock.GetUtcNow().UtcDateTime;

        _clock.Advance(TimeSpan.FromHours(1));
        using (var db = _database.CreateContext())
        {
            await new DisburseLoanHandler(db, _clock).HandleAsync(new DisburseLoanCommand(id), _ct);
        }
        var disbursedAt = _clock.GetUtcNow().UtcDateTime;

        using var readDb = _database.CreateContext();
        var loan = await new GetLoanByIdHandler(readDb).HandleAsync(new GetLoanByIdQuery(id), _ct);

        Assert.Equal(LoanStatus.Disbursed, loan.Status);
        Assert.Equal(approvedAt, loan.ApprovedAtUtc);
        Assert.Equal(disbursedAt, loan.DisbursedAtUtc);
    }

    [Fact]
    public async Task Disburse_PendingLoan_ThrowsAndLeavesDatabaseUnchanged()
    {
        var id = await CreateLoanAsync();

        using (var db = _database.CreateContext())
        {
            var handler = new DisburseLoanHandler(db, _clock);
            await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new DisburseLoanCommand(id), _ct));
        }

        using var readDb = _database.CreateContext();
        var loan = await new GetLoanByIdHandler(readDb).HandleAsync(new GetLoanByIdQuery(id), _ct);
        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Null(loan.DisbursedAtUtc);
    }

    [Fact]
    public async Task Reject_SavesReason()
    {
        var id = await CreateLoanAsync();

        using (var db = _database.CreateContext())
        {
            await new RejectLoanHandler(db, _clock).HandleAsync(new RejectLoanCommand(id, "Insufficient income"), _ct);
        }

        using var readDb = _database.CreateContext();
        var loan = await new GetLoanByIdHandler(readDb).HandleAsync(new GetLoanByIdQuery(id), _ct);
        Assert.Equal(LoanStatus.Rejected, loan.Status);
        Assert.Equal("Insufficient income", loan.RejectionReason);
    }

    [Fact]
    public async Task Approve_UnknownLoan_ThrowsNotFound()
    {
        using var db = _database.CreateContext();
        var handler = new ApproveLoanHandler(db, _clock);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new ApproveLoanCommand(Guid.NewGuid()), _ct));
    }

    [Fact]
    public async Task GetLoanById_UnknownLoan_ThrowsNotFound()
    {
        using var db = _database.CreateContext();
        var handler = new GetLoanByIdHandler(db);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new GetLoanByIdQuery(Guid.NewGuid()), _ct));
    }

    [Fact]
    public async Task GetLoans_FiltersByStatus_AndReturnsNewestFirst()
    {
        var first = await CreateLoanAsync("First");
        _clock.Advance(TimeSpan.FromMinutes(1));
        var second = await CreateLoanAsync("Second");
        _clock.Advance(TimeSpan.FromMinutes(1));
        var third = await CreateLoanAsync("Third");

        using (var db = _database.CreateContext())
        {
            await new ApproveLoanHandler(db, _clock).HandleAsync(new ApproveLoanCommand(second), _ct);
        }

        using var readDb = _database.CreateContext();
        var handler = new GetLoansHandler(readDb);

        var approved = await handler.HandleAsync(new GetLoansQuery(LoanStatus.Approved), _ct);
        var all = await handler.HandleAsync(new GetLoansQuery(null), _ct);

        Assert.Equal(new[] { second }, approved.Items.Select(l => l.Id));
        Assert.Equal(new[] { third, second, first }, all.Items.Select(l => l.Id));
    }
}
