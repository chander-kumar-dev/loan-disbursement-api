using LoanDisbursement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LoanDisbursement.Tests.TestSupport;

/// <summary>
/// An in-memory SQLite database that lives as long as this object.
/// Each CreateContext() call returns a fresh AppDbContext, like a new HTTP request would,
/// so tests prove data was really saved and not just kept in memory by EF Core.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
