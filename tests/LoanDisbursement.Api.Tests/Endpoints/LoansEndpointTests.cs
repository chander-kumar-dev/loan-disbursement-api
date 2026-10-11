using System.Net;
using System.Net.Http.Json;
using LoanDisbursement.Api.Tests.Infrastructure;
using LoanDisbursement.Application.Loans;
using LoanDisbursement.Domain.Loans;
using Microsoft.AspNetCore.Mvc;

namespace LoanDisbursement.Api.Tests.Endpoints;

/// <summary>
/// Tests through HTTP: routing, model binding, validation, status codes and the problem-details format,
/// none of which the handler tests cover.
/// </summary>
public class LoansEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public LoansEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    private async Task<LoanDto> CreateLoanAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/loans", TestRequests.NewLoan());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<LoanDto>();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedTitle)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.Equal(expectedTitle, problem.Title);
    }

    [Fact]
    public async Task Create_Returns201_WithTheNewLoansLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/loans", TestRequests.NewLoan());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<LoanDto>();
        Assert.Equal(LoanStatus.Pending, created.Status);
        Assert.NotNull(response.Headers.Location);

        var fetched = await (await _client.GetAsync(response.Headers.Location)).ReadAsync<LoanDto>();
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task Create_NormalisesTheIban()
    {
        var response = await _client.PostAsJsonAsync("/api/loans",
            TestRequests.NewLoan(accountNumber: "pk36 scbl 0000 0011 2345 6702"));

        var loan = await response.ReadAsync<LoanDto>();
        Assert.Equal(TestRequests.ValidIban, loan.AccountNumber);
    }

    [Fact]
    public async Task FullLifecycle_CreateApproveDisburse()
    {
        var loan = await CreateLoanAsync();

        var approved = await _client.PostAsync($"/api/loans/{loan.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var disbursed = await _client.PostAsync($"/api/loans/{loan.Id}/disburse", null);
        Assert.Equal(HttpStatusCode.OK, disbursed.StatusCode);

        var result = await disbursed.ReadAsync<LoanDto>();
        Assert.Equal(LoanStatus.Disbursed, result.Status);
        Assert.NotNull(result.DisbursedAtUtc);
    }

    [Fact]
    public async Task GetById_UnknownLoan_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/loans/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Not found");
    }

    [Fact]
    public async Task Disburse_PendingLoan_Returns409()
    {
        var loan = await CreateLoanAsync();

        var response = await _client.PostAsync($"/api/loans/{loan.Id}/disburse", null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Business rule violated");
    }

    [Fact]
    public async Task Disburse_Twice_SecondReturns409()
    {
        var loan = await CreateLoanAsync();
        await _client.PostAsync($"/api/loans/{loan.Id}/approve", null);
        await _client.PostAsync($"/api/loans/{loan.Id}/disburse", null);

        var second = await _client.PostAsync($"/api/loans/{loan.Id}/disburse", null);

        await AssertProblemAsync(second, HttpStatusCode.Conflict, "Business rule violated");
    }

    [Fact]
    public async Task Create_MissingApplicantName_Returns400WithFieldError()
    {
        var response = await _client.PostAsJsonAsync("/api/loans", TestRequests.NewLoan(applicantName: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains(problem.Errors.Keys, key => key.Equals("ApplicantName", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_InvalidIban_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/loans",
            TestRequests.NewLoan(accountNumber: "PK36SCBL0000001123456703"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation failed");
    }

    [Fact]
    public async Task Create_AmountAboveProductLimit_Returns400_Not409()
    {
        var response = await _client.PostAsJsonAsync("/api/loans", TestRequests.NewLoan(amount: 5_000_001m));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation failed");
    }

    [Fact]
    public async Task Reject_WithReason_Returns200()
    {
        var loan = await CreateLoanAsync();

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/reject", new { reason = "Insufficient income" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rejected = await response.ReadAsync<LoanDto>();
        Assert.Equal(LoanStatus.Rejected, rejected.Status);
        Assert.Equal("Insufficient income", rejected.RejectionReason);
    }
}
