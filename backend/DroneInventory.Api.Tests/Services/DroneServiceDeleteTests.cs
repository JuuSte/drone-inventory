using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-management: Delete Drone (REQ-011).</summary>
public class DroneServiceDeleteTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 8, 19);

    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task DeleteAsync(int id)
    {
        await using var context = _db.CreateContext();
        await new DroneService(context).DeleteAsync(id);
    }

    private async Task<IReadOnlyList<DroneDto>> GetAllAsync()
    {
        await using var context = _db.CreateContext();
        return await new DroneService(context).GetAllAsync();
    }

    /// <summary>Stores a Drone with one Loan per given status (all to "Group 3") and returns its Id.</summary>
    private async Task<int> SeedDroneAsync(string name, params LoanStatus[] loans)
    {
        await using var context = _db.CreateContext();
        var group = await context.StudentGroups.SingleOrDefaultAsync(g => g.Name == "Group 3")
            ?? new StudentGroup { Name = "Group 3" };
        var drone = new Drone { Name = name, Description = $"{name} description" };
        foreach (var status in loans)
        {
            drone.Loans.Add(new Loan
            {
                StudentGroup = group,
                LoanDate = Today,
                ReturnDate = status == LoanStatus.Returned ? Today : null,
                Status = status,
                Items = [new LoanItem { Type = LoanItemType.Battery, Quantity = 4 }]
            });
        }
        context.Drones.Add(drone);
        await context.SaveChangesAsync();
        return drone.Id;
    }

    private async Task<int> CountLoansAsync(int droneId)
    {
        await using var context = _db.CreateContext();
        return await context.Loans.CountAsync(l => l.DroneId == droneId);
    }

    // GIVEN the Drone "TELLO-08" has no Loans
    // WHEN "TELLO-08" is deleted
    // THEN the Drone is removed AND "TELLO-08" is no longer listed
    [Fact]
    public async Task Drone_without_Loans_is_deleted()
    {
        await SeedDroneAsync("TELLO-07", LoanStatus.Returned);
        var id = await SeedDroneAsync("TELLO-08");

        await DeleteAsync(id);

        await using (var verify = _db.CreateContext())
        {
            Assert.Null(await verify.Drones.FindAsync(id));
        }
        var remaining = Assert.Single(await GetAllAsync());
        Assert.Equal("TELLO-07", remaining.Name);
    }

    // GIVEN the Drone "TELLO-07" has a Loan with Status "Returned"
    // WHEN "TELLO-07" is deleted
    // THEN the request is rejected as a conflict AND the Drone "TELLO-07" and its Loans remain stored
    [Fact]
    public async Task Drone_with_loan_history()
    {
        var id = await SeedDroneAsync("TELLO-07", LoanStatus.Returned);

        var error = await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(id));

        Assert.Equal("Drone \"TELLO-07\" cannot be deleted because it has Loans.", error.Message);
        Assert.Equal("TELLO-07", Assert.Single(await GetAllAsync()).Name);
        Assert.Equal(1, await CountLoansAsync(id));
        await using var verify = _db.CreateContext();
        Assert.Equal(1, await verify.LoanItems.CountAsync());
    }

    // GIVEN the Drone "TELLO-07" has an active Loan
    // WHEN "TELLO-07" is deleted
    // THEN the request is rejected as a conflict AND "TELLO-07" is still listed as "Loaned"
    [Fact]
    public async Task Drone_with_an_active_Loan()
    {
        var id = await SeedDroneAsync("TELLO-07", LoanStatus.Active);

        await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(id));

        var drone = Assert.Single(await GetAllAsync());
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal(DroneStatus.Loaned, drone.Status);
        Assert.Equal(1, await CountLoansAsync(id));
    }

    // GIVEN no Drone with the requested Id exists
    // WHEN that Drone is deleted
    // THEN the request is rejected as not found
    [Fact]
    public async Task Unknown_Drone()
    {
        var id = await SeedDroneAsync("TELLO-07");

        var error = await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(id + 1));

        Assert.Equal($"Drone with Id {id + 1} was not found.", error.Message);
        Assert.Single(await GetAllAsync());
    }

    // Same as "Drone with an active Loan", but the Loan is created after the service check
    // (concurrent request): the database refuses the delete and it is mapped to the same conflict (design D2).
    [Fact]
    public async Task Drone_loaned_by_a_concurrent_request_is_not_deleted()
    {
        var id = await SeedDroneAsync("TELLO-08");
        int groupId;
        await using (var context = _db.CreateContext())
        {
            var group = new StudentGroup { Name = "Group 5" };
            context.StudentGroups.Add(group);
            await context.SaveChangesAsync();
            groupId = group.Id;
        }
        var interceptor = new InsertCompetingLoanInterceptor(id, groupId);

        await using (var context = _db.CreateContext(interceptor))
        {
            await Assert.ThrowsAsync<ConflictException>(() => new DroneService(context).DeleteAsync(id));
        }

        Assert.True(interceptor.Inserted);
        Assert.Equal(DroneStatus.Loaned, Assert.Single(await GetAllAsync()).Status);
        Assert.Equal(1, await CountLoansAsync(id));
    }

    /// <summary>Simulates a concurrent request by inserting an active Loan right before the service saves the delete.</summary>
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
