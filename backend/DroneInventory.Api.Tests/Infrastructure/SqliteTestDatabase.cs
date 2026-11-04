using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace DroneInventory.Api.Tests.Infrastructure;

/// <summary>
/// Test helper (design D6): provides a DbContext on SQLite in-memory and a fake TimeProvider.
/// The connection stays open for the lifetime of the helper, so the in-memory database
/// survives across contexts. Real relational behavior (unique and filtered indexes) is used.
/// </summary>
/// <example>
/// using var db = new SqliteTestDatabase&lt;AppDbContext&gt;(options =&gt; new AppDbContext(options));
/// await using var context = db.CreateContext();
/// </example>
public sealed class SqliteTestDatabase<TContext> : IDisposable where TContext : DbContext
{
    /// <summary>Fixed default "now" used by tests: 19.08.2026 10:00 UTC.</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly Func<DbContextOptions<TContext>, TContext> _factory;
    private readonly DbContextOptions<TContext> _options;

    public SqliteTestDatabase(Func<DbContextOptions<TContext>, TContext> factory, DateTimeOffset? now = null)
    {
        _factory = factory;
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection)
            .Options;

        TimeProvider = new FakeTimeProvider(now ?? DefaultNow);

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Fake clock for services that need the current date (LoanDate, ReturnDate).</summary>
    public FakeTimeProvider TimeProvider { get; }

    /// <summary>Creates a new context on the shared in-memory database.</summary>
    public TContext CreateContext() => _factory(_options);

    public void Dispose() => _connection.Dispose();
}
