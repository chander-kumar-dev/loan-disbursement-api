using LoanDisbursement.Api.Contracts;
using LoanDisbursement.Application.Abstractions;
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

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LoanDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetAll(
        [FromQuery] LoanStatus? status,
        [FromServices] IQueryHandler<GetLoansQuery, IReadOnlyList<LoanDto>> handler,
        CancellationToken cancellationToken)
    {
        var loans = await handler.HandleAsync(new GetLoansQuery(status), cancellationToken);
        return Ok(loans);
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
