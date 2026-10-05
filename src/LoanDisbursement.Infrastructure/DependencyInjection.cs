using LoanDisbursement.Application.Abstractions;
using LoanDisbursement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanDisbursement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LoanDb")
            ?? throw new InvalidOperationException("Connection string 'LoanDb' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // Handlers ask for IAppDbContext; give them the same AppDbContext instance
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        return services;
    }
}
