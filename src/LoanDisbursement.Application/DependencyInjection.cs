using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Application.Common;
using LoanDisbursement.Application.Loans;
using LoanDisbursement.Application.Loans.Commands;
using LoanDisbursement.Application.Loans.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace LoanDisbursement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // Commands (write side)
        services.AddScoped<ICommandHandler<CreateLoanCommand, LoanDto>, CreateLoanHandler>();
        services.AddScoped<ICommandHandler<ApproveLoanCommand, LoanDto>, ApproveLoanHandler>();
        services.AddScoped<ICommandHandler<RejectLoanCommand, LoanDto>, RejectLoanHandler>();
        services.AddScoped<ICommandHandler<DisburseLoanCommand, LoanDto>, DisburseLoanHandler>();

        // Queries (read side)
        services.AddScoped<IQueryHandler<GetLoanByIdQuery, LoanDto>, GetLoanByIdHandler>();
        services.AddScoped<IQueryHandler<GetLoansQuery, PagedResult<LoanDto>>, GetLoansHandler>();

        return services;
    }
}
