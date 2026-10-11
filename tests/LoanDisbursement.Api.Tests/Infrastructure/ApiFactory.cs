using LoanDisbursement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LoanDisbursement.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory against an in-memory SQLite database.
/// Each factory has its own database, which lives as long as the factory.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();

        // AddInfrastructure throws when the connection string is missing, and Program reads configuration
        // before the test host's own settings are applied. An environment variable is read first, so it is
        // the reliable way to provide one. The SQL Server registration it creates is replaced below.
        Environment.SetEnvironmentVariable("ConnectionStrings__LoanDb", "Server=not-used-by-tests;Database=not-used");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            RemoveSqlServerOptions(services);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    /// <summary>An HTTP client for the in-memory API.</summary>
    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    /// <summary>
    /// Removes the SQL Server options for AppDbContext. Since EF Core 9, AddDbContext registers both
    /// DbContextOptions&lt;AppDbContext&gt; and a separate IDbContextOptionsConfiguration&lt;AppDbContext&gt; callback.
    /// Removing only the first leaves UseSqlServer in place and fails with
    /// "services for database providers 'SqlServer', 'Sqlite' have been registered".
    /// </summary>
    private static void RemoveSqlServerOptions(IServiceCollection services)
    {
        var registrations = services
            .Where(d => d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericArguments().SequenceEqual(new[] { typeof(AppDbContext) })
                        && (d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)
                            || d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"))
            .ToList();

        foreach (var registration in registrations)
        {
            services.Remove(registration);
        }
    }
}
