using LoanDisbursement.Application.Common;
using LoanDisbursement.Application.Loans;
using LoanDisbursement.Application.Loans.Commands;
using LoanDisbursement.Application.Loans.Queries;
using LoanDisbursement.Domain.Loans;
using LoanDisbursement.Tests.TestSupport;

namespace LoanDisbursement.Tests.Application;

public class GetLoansPaginationTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly CancellationToken _ct = CancellationToken.None;

    public void Dispose() => _database.Dispose();

    private async Task<List<Guid>> CreateLoansAsync(int count, TimeSpan gap)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            using var db = _database.CreateContext();
            var loan = await new CreateLoanHandler(db, _clock, TestData.Limits)
                .HandleAsync(new CreateLoanCommand($"Applicant {i}", TestData.ValidIban, 10_000m, 12), _ct);
            ids.Add(loan.Id);
            _clock.Advance(gap);
        }
        return ids;
    }

    private async Task<List<PagedResult<LoanDto>>> ReadAllPagesAsync(int pageSize, LoanStatus? status = null)
    {
        var pages = new List<PagedResult<LoanDto>>();
        string? cursor = null;
        do
        {
            using var db = _database.CreateContext();
            var page = await new GetLoansHandler(db).HandleAsync(new GetLoansQuery(status, pageSize, cursor), _ct);
            pages.Add(page);
            cursor = page.NextCursor;
        }
        while (cursor is not null && pages.Count < 100); // guard against an endless loop if paging is broken

        return pages;
    }

    [Fact]
    public async Task Paging_ReturnsEveryLoanOnce_NewestFirst()
    {
        var created = await CreateLoansAsync(25, TimeSpan.FromMinutes(1));

        var pages = await ReadAllPagesAsync(pageSize: 10);

        Assert.Equal(new[] { 10, 10, 5 }, pages.Select(p => p.Items.Count));
        Assert.Null(pages[^1].NextCursor);

        var ids = pages.SelectMany(p => p.Items).Select(l => l.Id).ToList();
        Assert.Equal(Enumerable.Reverse(created), ids);
    }

    [Fact]
    public async Task Paging_LoansCreatedAtTheSameInstant_AreNeitherSkippedNorRepeated()
    {
        // No clock advance: every loan has the same CreatedAtUtc, so only the Id tie-breaker separates them.
        var created = await CreateLoansAsync(5, TimeSpan.Zero);

        var pages = await ReadAllPagesAsync(pageSize: 2);

        var ids = pages.SelectMany(p => p.Items).Select(l => l.Id).ToList();
        Assert.Equal(5, ids.Count);
        Assert.Equal(created.OrderBy(id => id), ids.OrderBy(id => id));
    }

    [Fact]
    public async Task Paging_WithStatusFilter_OnlyReturnsMatchingLoans()
    {
        var created = await CreateLoansAsync(6, TimeSpan.FromMinutes(1));
        foreach (var id in created.Where((_, index) => index % 2 == 0))
        {
            using var db = _database.CreateContext();
            await new ApproveLoanHandler(db, _clock).HandleAsync(new ApproveLoanCommand(id), _ct);
        }

        var pages = await ReadAllPagesAsync(pageSize: 2, LoanStatus.Approved);

        var items = pages.SelectMany(p => p.Items).ToList();
        Assert.Equal(3, items.Count);
        Assert.All(items, l => Assert.Equal(LoanStatus.Approved, l.Status));
    }

    [Fact]
    public async Task ExactlyOneFullPage_HasNoNextCursor()
    {
        await CreateLoansAsync(3, TimeSpan.FromMinutes(1));

        using var db = _database.CreateContext();
        var page = await new GetLoansHandler(db).HandleAsync(new GetLoansQuery(null, PageSize: 3), _ct);

        Assert.Equal(3, page.Items.Count);
        Assert.Null(page.NextCursor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetLoansQuery.MaxPageSize + 1)]
    public async Task PageSizeOutOfRange_Throws(int pageSize)
    {
        using var db = _database.CreateContext();
        var handler = new GetLoansHandler(db);

        await Assert.ThrowsAsync<RequestValidationException>(() => handler.HandleAsync(new GetLoansQuery(null, pageSize), _ct));
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("")]
    [InlineData("MTIzfG5vdC1hLWd1aWQ")] // valid Base64Url of "123|not-a-guid"
    public async Task InvalidCursor_Throws(string cursor)
    {
        using var db = _database.CreateContext();
        var handler = new GetLoansHandler(db);

        await Assert.ThrowsAsync<RequestValidationException>(() => handler.HandleAsync(new GetLoansQuery(null, 10, cursor), _ct));
    }

    [Fact]
    public void Cursor_RoundTrips()
    {
        var original = new LoanCursor(new DateTime(2026, 10, 1, 9, 0, 0, 123, DateTimeKind.Utc), Guid.NewGuid());

        var decoded = LoanCursor.Decode(LoanCursor.Encode(original));

        Assert.Equal(original, decoded);
    }
}
