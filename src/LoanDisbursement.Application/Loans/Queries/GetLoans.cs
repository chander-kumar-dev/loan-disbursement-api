using System.Globalization;
using System.Text;
using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Application.Loans.Queries;

/// <param name="Status">Optional filter. Null returns loans in every status.</param>
/// <param name="PageSize">Items per page, 1 to <see cref="GetLoansQuery.MaxPageSize"/>.</param>
/// <param name="Cursor">The <c>NextCursor</c> from the previous page, or null for the first page.</param>
public record GetLoansQuery(LoanStatus? Status, int PageSize = GetLoansQuery.DefaultPageSize, string? Cursor = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>
/// Lists loans newest first using keyset (cursor) pagination.
/// Unlike Skip/Take, the cost of a page does not grow with how deep the client pages,
/// and rows inserted while paging do not shift later pages.
/// </summary>
public class GetLoansHandler : IQueryHandler<GetLoansQuery, PagedResult<LoanDto>>
{
    private readonly IAppDbContext _db;

    public GetLoansHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<LoanDto>> HandleAsync(GetLoansQuery query, CancellationToken cancellationToken)
    {
        if (query.PageSize < 1 || query.PageSize > GetLoansQuery.MaxPageSize)
        {
            throw new RequestValidationException($"pageSize must be between 1 and {GetLoansQuery.MaxPageSize}.");
        }

        var loans = _db.Loans.AsNoTracking();

        if (query.Status is not null)
        {
            loans = loans.Where(l => l.Status == query.Status);
        }

        if (query.Cursor is not null)
        {
            var cursor = LoanCursor.Decode(query.Cursor);
            var createdAt = cursor.CreatedAtUtc;
            var id = cursor.Id;

            // Rows strictly after the cursor in (CreatedAtUtc DESC, Id DESC) order.
            // Id breaks ties between loans created at the same instant.
            loans = loans.Where(l => l.CreatedAtUtc < createdAt
                                     || (l.CreatedAtUtc == createdAt && l.Id.CompareTo(id) < 0));
        }

        // Fetch one extra row: if it exists, there is a next page.
        var page = await loans
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (page.Count > query.PageSize)
        {
            page.RemoveAt(page.Count - 1);
            var last = page[^1];
            nextCursor = LoanCursor.Encode(new LoanCursor(last.CreatedAtUtc, last.Id));
        }

        return new PagedResult<LoanDto>(page.Select(LoanDto.FromLoan).ToList(), nextCursor);
    }
}

/// <summary>
/// The position of the last loan on a page. Sent to clients as an opaque Base64Url string,
/// so they cannot depend on its format.
/// </summary>
public record LoanCursor(DateTime CreatedAtUtc, Guid Id)
{
    public static string Encode(LoanCursor cursor)
    {
        var raw = string.Create(CultureInfo.InvariantCulture, $"{cursor.CreatedAtUtc.Ticks}|{cursor.Id:N}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static LoanCursor Decode(string value)
    {
        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(base64)).Split('|');
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                && ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
                && Guid.TryParseExact(parts[1], "N", out var id))
            {
                return new LoanCursor(new DateTime(ticks, DateTimeKind.Utc), id);
            }
        }
        catch (FormatException)
        {
            // Not valid Base64; reported below like any other bad cursor.
        }

        throw new RequestValidationException("The cursor is invalid. Use the nextCursor value returned by the previous page.");
    }
}
