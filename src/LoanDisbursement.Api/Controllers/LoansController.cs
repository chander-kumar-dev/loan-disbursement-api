using LoanDisbursement.Api.Contracts;
using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Application.Loans;
using LoanDisbursement.Application.Loans.Commands;
using LoanDisbursement.Application.Loans.Queries;
using LoanDisbursement.Domain.Loans;
using Microsoft.AspNetCore.Mvc;

namespace LoanDisbursement.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    // Each action receives only the handler it needs ([FromServices]),
    // so the controller stays thin and every use case is its own class.

    [HttpPost]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoanDto>> Create(
        CreateLoanRequest request,
        [FromServices] ICommandHandler<CreateLoanCommand, LoanDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateLoanCommand(request.ApplicantName, request.AccountNumber, request.Amount, request.TenureMonths);
        var loan = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetLoanByIdQuery, LoanDto> handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new GetLoanByIdQuery(id), cancellationToken);
    }

    /// <summary>
    /// Lists loans newest first, one page at a time. To get the next page, pass the
    /// <c>nextCursor</c> from the response as <c>cursor</c>. It is null on the last page.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<LoanDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LoanDto>>> GetAll(
        [FromServices] IQueryHandler<GetLoansQuery, PagedResult<LoanDto>> handler,
        [FromQuery] LoanStatus? status = null,
        [FromQuery] int pageSize = GetLoansQuery.DefaultPageSize,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var page = await handler.HandleAsync(new GetLoansQuery(status, pageSize, cursor), cancellationToken);
        return Ok(page);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Approve(
        Guid id,
        [FromServices] ICommandHandler<ApproveLoanCommand, LoanDto> handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new ApproveLoanCommand(id), cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Reject(
        Guid id,
        RejectLoanRequest request,
        [FromServices] ICommandHandler<RejectLoanCommand, LoanDto> handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new RejectLoanCommand(id, request.Reason), cancellationToken);
    }

    [HttpPost("{id:guid}/disburse")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Disburse(
        Guid id,
        [FromServices] ICommandHandler<DisburseLoanCommand, LoanDto> handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new DisburseLoanCommand(id), cancellationToken);
    }
}
