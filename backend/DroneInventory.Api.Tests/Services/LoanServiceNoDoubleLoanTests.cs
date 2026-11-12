using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-lending: No double Loan (REQ-005).</summary>
public class LoanServiceNoDoubleLoanTests : IDisposable
{
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<(int DroneId, int FirstGroupId, int SecondGroupId)> SeedAsync()
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = "TELLO-07", Description = "Tello" };
        var first = new StudentGroup { Name = "Group 3" };
        var second = new StudentGroup { Name = "Group 4" };
        context.AddRange(drone, first, second);
        await context.SaveChangesAsync();
        return (drone.Id, first.Id, second.Id);
    }

    private async Task<LoanDto> CreateAsync(AppDbContext context, int droneId, int groupId) =>
        await new LoanService(context, _db.TimeProvider).CreateAsync(new CreateLoanRequest(droneId, groupId));

    private async Task<int> CountActiveLoansAsync(int droneId)
    {
        await using var context = _db.CreateContext();
        return await context.Loans.CountAsync(l => l.DroneId == droneId && l.Status == LoanStatus.Active);
    }

    // GIVEN "TELLO-07" already has an active Loan
    // WHEN another StudentGroup tries to loan "TELLO-07"
    // THEN the Loan is rejected AND "TELLO-07" still has exactly one active Loan
    [Fact]
    public async Task Drone_is_already_loaned()
    {
        var (droneId, firstGroupId, secondGroupId) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            await CreateAsync(context, droneId, firstGroupId);
        }

        await using (var context = _db.CreateContext())
        {
            var exception = await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(context, droneId, secondGroupId));
            Assert.Contains("TELLO-07", exception.Message);
        }

        Assert.Equal(1, await CountActiveLoansAsync(droneId));
    }

    // Same scenario, but the competing Loan is inserted after the service check (concurrent request):
    // the DbUpdateException from the filtered unique index is mapped to the same conflict (design D2).
    // The test database is a single shared in-memory connection, so the competing insert runs inside the
    // service's Loan transaction (soft-delete design D4) and is rolled back with it. In production a second
    // request would use its own connection and be blocked by that transaction's write lock instead.
    // So this test checks the conflict mapping and that the rejected request stored no Loan of its own.
    [Fact]
    public async Task Drone_is_already_loaned_by_a_concurrent_request()
    {
        var (droneId, firstGroupId, secondGroupId) = await SeedAsync();
        var interceptor = new InsertCompetingLoanInterceptor(droneId, firstGroupId);

        await using (var context = _db.CreateContext(interceptor))
        {
            await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(context, droneId, secondGroupId));
        }

        Assert.True(interceptor.Inserted);
        await using var verify = _db.CreateContext();
        Assert.False(await verify.Loans.AnyAsync(l => l.DroneId == droneId && l.StudentGroupId == secondGroupId));
        // The competing insert shared the rolled-back transaction, so no active Loan is left at all.
        Assert.Equal(0, await CountActiveLoansAsync(droneId));
    }

    [Fact]
    public async Task Drone_with_only_returned_Loans_can_be_loaned_again()
    {
        var (droneId, firstGroupId, secondGroupId) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(new Loan
            {
                DroneId = droneId, StudentGroupId = firstGroupId, LoanDate = new DateOnly(2026, 8, 1),
                ReturnDate = new DateOnly(2026, 8, 2), Status = LoanStatus.Returned
            });
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            await CreateAsync(context, droneId, secondGroupId);
        }

        Assert.Equal(1, await CountActiveLoansAsync(droneId));
    }

    /// <summary>Simulates a concurrent request by inserting an active Loan right before the service saves its own.</summary>
    private sealed class InsertCompetingLoanInterceptor(int droneId, int groupId) : SaveChangesInterceptor
    {
        public bool Inserted { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Inserted)
            {
                Inserted = true;
                await eventData.Context!.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"Loans\" (\"DroneId\", \"StudentGroupId\", \"LoanDate\", \"Status\") VALUES ({droneId}, {groupId}, '2026-08-19', 'Active')",
                    cancellationToken);
            }
            return result;
        }
    }
}
