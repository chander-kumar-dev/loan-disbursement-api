namespace LoanDisbursement.Application.Common;

/// <summary>
/// One page of results. Pass <see cref="NextCursor"/> back to get the next page;
/// it is null on the last page.
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor);
