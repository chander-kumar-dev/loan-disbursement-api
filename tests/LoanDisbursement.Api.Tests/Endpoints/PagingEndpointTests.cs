using System.Net;
using System.Net.Http.Json;
using LoanDisbursement.Api.Tests.Infrastructure;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Application.Loans;

namespace LoanDisbursement.Api.Tests.Endpoints;

public class PagingEndpointTests
{
    [Fact]
    public async Task Paging_ThroughHttp_ReturnsEveryLoanOnce()
    {
        // Own factory, so the database holds only this test's loans.
        using var factory = new ApiFactory();
        var client = factory.CreateApiClient();

        var created = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/loans", TestRequests.NewLoan(applicantName: $"Applicant {i}"));
            created.Add((await response.ReadAsync<LoanDto>()).Id);
        }

        var seen = new List<Guid>();
        var pageSizes = new List<int>();
        string? cursor = null;
        do
        {
            var url = cursor is null ? "/api/loans?pageSize=2" : $"/api/loans?pageSize=2&cursor={Uri.EscapeDataString(cursor)}";
            var page = await (await client.GetAsync(url)).ReadAsync<PagedResult<LoanDto>>();
            pageSizes.Add(page.Items.Count);
            seen.AddRange(page.Items.Select(l => l.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null && pageSizes.Count < 10);

        Assert.Equal(new[] { 2, 2, 1 }, pageSizes);
        Assert.Equal(created.OrderBy(id => id), seen.OrderBy(id => id));
    }

    [Theory]
    [InlineData("/api/loans?pageSize=0")]
    [InlineData("/api/loans?pageSize=101")]
    [InlineData("/api/loans?cursor=not-a-real-cursor")]
    public async Task InvalidPagingParameters_Return400(string url)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateApiClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
