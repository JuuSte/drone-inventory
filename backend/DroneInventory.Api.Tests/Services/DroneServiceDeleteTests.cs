using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-management: Delete Drone (REQ-011), soft delete (design D3).</summary>
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

    private async Task<IReadOnlyList<LoanDto>> GetHistoryAsync()
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).GetHistoryAsync();
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
                Items =
                [
                    new LoanItem { Type = LoanItemType.Battery, Quantity = 4 },
                    new LoanItem { Type = LoanItemType.Propeller, Quantity = 8 }
                ]
            });
        }
        context.Drones.Add(drone);
        await context.SaveChangesAsync();
        return drone.Id;
    }

    private async Task<Drone?> FindStoredAsync(int id)
    {
        await using var context = _db.CreateContext();
        return await context.Drones.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id);
    }

    private async Task<int> CountLoansAsync(int droneId)
    {
        await using var context = _db.CreateContext();
        return await context.Loans.CountAsync(l => l.DroneId == droneId);
    }

    // GIVEN the Drone "TELLO-07" has only returned Loans
    // WHEN "TELLO-07" is deleted
    // THEN "TELLO-07" is no longer listed
    // AND the loan history still shows the Loans of "TELLO-07" with the Drone Name "TELLO-07"
    [Fact]
    public async Task Drone_with_loan_history()
    {
        var id = await SeedDroneAsync("TELLO-07", LoanStatus.Returned, LoanStatus.Returned);
        await SeedDroneAsync("TELLO-08");

        await DeleteAsync(id);

        Assert.Equal("TELLO-08", Assert.Single(await GetAllAsync()).Name);
        var history = await GetHistoryAsync();
        Assert.Equal(2, history.Count);
        Assert.All(history, loan =>
        {
            Assert.Equal(id, loan.DroneId);
            Assert.Equal("TELLO-07", loan.DroneName);
            Assert.Equal("Group 3", loan.StudentGroupName);
            Assert.Equal(4, loan.Batteries);
            Assert.Equal(8, loan.Propellers);
        });

        // Soft delete: the Drone row and its Loans and LoanItems stay stored unchanged (REQ-010).
        var stored = await FindStoredAsync(id);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
        Assert.Equal(2, await CountLoansAsync(id));
        await using var verify = _db.CreateContext();
        Assert.Equal(4, await verify.LoanItems.CountAsync());
    }

    // GIVEN the Drone "TELLO-08" has no Loans
    // WHEN "TELLO-08" is deleted
    // THEN "TELLO-08" is no longer listed
    [Fact]
    public async Task Drone_without_Loans_is_deleted()
    {
        await SeedDroneAsync("TELLO-07", LoanStatus.Returned);
        var id = await SeedDroneAsync("TELLO-08");

        await DeleteAsync(id);

        Assert.Equal("TELLO-07", Assert.Single(await GetAllAsync()).Name);
        Assert.True((await FindStoredAsync(id))!.IsDeleted);
    }

    // GIVEN the Drone "TELLO-07" has an active Loan
    // WHEN "TELLO-07" is deleted
    // THEN the request is rejected as a conflict AND "TELLO-07" is still listed as "Loaned"
    [Fact]
    public async Task Drone_with_an_active_Loan()
    {
        var id = await SeedDroneAsync("TELLO-07", LoanStatus.Returned, LoanStatus.Active);

        var error = await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(id));

        Assert.Equal("Drone \"TELLO-07\" cannot be deleted because it has an active Loan.", error.Message);
        var drone = Assert.Single(await GetAllAsync());
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal(DroneStatus.Loaned, drone.Status);
        Assert.False((await FindStoredAsync(id))!.IsDeleted);
        Assert.Equal(2, await CountLoansAsync(id));
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

    // GIVEN the Drone "TELLO-08" was deleted
    // WHEN "TELLO-08" is deleted again
    // THEN the request is rejected as not found
    [Fact]
    public async Task Drone_is_deleted_twice()
    {
        var id = await SeedDroneAsync("TELLO-08");
        await DeleteAsync(id);

        var error = await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(id));

        Assert.Equal($"Drone with Id {id} was not found.", error.Message);
        Assert.True((await FindStoredAsync(id))!.IsDeleted);
    }
}
