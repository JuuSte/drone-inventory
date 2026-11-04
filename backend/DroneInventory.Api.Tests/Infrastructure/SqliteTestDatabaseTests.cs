using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Infrastructure;

public class SqliteTestDatabaseTests
{
    private sealed class SmokeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SmokeContext(DbContextOptions<SmokeContext> options) : DbContext(options)
    {
        public DbSet<SmokeEntity> Entities => Set<SmokeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<SmokeEntity>().HasIndex(e => e.Name).IsUnique();
    }

    [Fact]
    public async Task Data_is_shared_between_contexts_of_the_same_database()
    {
        using var db = new SqliteTestDatabase<SmokeContext>(o => new SmokeContext(o));

        await using (var write = db.CreateContext())
        {
            write.Entities.Add(new SmokeEntity { Name = "TELLO-01" });
            await write.SaveChangesAsync();
        }

        await using var read = db.CreateContext();
        Assert.Equal("TELLO-01", (await read.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task Unique_indexes_are_enforced_by_sqlite()
    {
        using var db = new SqliteTestDatabase<SmokeContext>(o => new SmokeContext(o));
        await using var context = db.CreateContext();

        context.Entities.AddRange(new SmokeEntity { Name = "TELLO-01" }, new SmokeEntity { Name = "TELLO-01" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public void Fake_time_provider_starts_at_fixed_date_and_can_advance()
    {
        using var db = new SqliteTestDatabase<SmokeContext>(o => new SmokeContext(o));

        Assert.Equal(new DateTimeOffset(2026, 8, 19, 10, 0, 0, TimeSpan.Zero), db.TimeProvider.GetUtcNow());

        db.TimeProvider.Advance(TimeSpan.FromDays(1));

        Assert.Equal(new DateOnly(2026, 8, 20), DateOnly.FromDateTime(db.TimeProvider.GetUtcNow().UtcDateTime));
    }
}
