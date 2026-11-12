using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-lending: Loan Drone (REQ-004).</summary>
public class LoanServiceCreateTests : IDisposable
{
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<LoanDto> CreateAsync(int droneId, int studentGroupId)
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).CreateAsync(new CreateLoanRequest(droneId, studentGroupId));
    }

    private async Task<(int DroneId, int StudentGroupId)> SeedAsync()
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = "TELLO-07", Description = "Tello" };
        var group = new StudentGroup { Name = "Group 3" };
        context.AddRange(drone, group);
        await context.SaveChangesAsync();
        return (drone.Id, group.Id);
    }

    private async Task<int> CountLoansAsync()
    {
        await using var context = _db.CreateContext();
        return await context.Loans.CountAsync();
    }

    // GIVEN the Drone "TELLO-07" is available AND the StudentGroup "Group 3" exists
    // WHEN "TELLO-07" is loaned to "Group 3"
    // THEN an active Loan is created with the current date as LoanDate
    // AND "TELLO-07" is shown as "Loaned"
    [Fact]
    public async Task Available_Drone_is_loaned()
    {
        var (droneId, groupId) = await SeedAsync();

        var loan = await CreateAsync(droneId, groupId);

        Assert.Equal("TELLO-07", loan.DroneName);
        Assert.Equal("Group 3", loan.StudentGroupName);
        Assert.Equal(nameof(LoanStatus.Active), loan.Status);
        Assert.Equal(DateOnly.FromDateTime(_db.TimeProvider.GetLocalNow().DateTime), loan.LoanDate);
        Assert.Null(loan.ReturnDate);

        await using var context = _db.CreateContext();
        var stored = await context.Loans.SingleAsync();
        Assert.Equal(LoanStatus.Active, stored.Status);
        Assert.Equal(droneId, stored.DroneId);
        Assert.Equal(groupId, stored.StudentGroupId);

        var drone = Assert.Single(await new DroneService(context).GetAllAsync());
        Assert.Equal(DroneStatus.Loaned, drone.Status);
    }

    [Fact]
    public async Task LoanDate_comes_from_TimeProvider()
    {
        var (droneId, groupId) = await SeedAsync();
        _db.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero));

        var loan = await CreateAsync(droneId, groupId);

        Assert.Equal(DateOnly.FromDateTime(_db.TimeProvider.GetLocalNow().DateTime), loan.LoanDate);
    }

    // GIVEN no Drone or no StudentGroup with the given Id exists
    // WHEN a Loan is created for it
    // THEN the Loan is rejected AND no Loan is stored
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Unknown_Drone_or_StudentGroup(bool unknownDrone, bool unknownGroup)
    {
        var (droneId, groupId) = await SeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateAsync(unknownDrone ? 999 : droneId, unknownGroup ? 999 : groupId));

        Assert.Equal(0, await CountLoansAsync());
    }

    // GIVEN the Drone "TELLO-07" was deleted and the StudentGroup "Group 3" exists
    // WHEN a Loan of "TELLO-07" to "Group 3" is created
    // THEN the request is rejected as not found AND no Loan is stored
    [Fact]
    public async Task Deleted_Drone_cannot_be_loaned()
    {
        var (droneId, groupId) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            await new DroneService(context).DeleteAsync(droneId);
        }

        var error = await Assert.ThrowsAsync<NotFoundException>(() => CreateAsync(droneId, groupId));

        Assert.Equal($"Drone with Id {droneId} was not found.", error.Message);
        Assert.Equal(0, await CountLoansAsync());
        await using var verify = _db.CreateContext();
        Assert.Equal(0, await verify.LoanItems.CountAsync());
    }

    // GIVEN the StudentGroup "Group 3" was deleted and the Drone "TELLO-07" is available
    // WHEN a Loan of "TELLO-07" to "Group 3" is created
    // THEN the request is rejected as not found AND "TELLO-07" stays "Available"
    [Fact]
    public async Task Deleted_StudentGroup_cannot_be_selected_for_a_Loan()
    {
        var (droneId, groupId) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            await new StudentGroupService(context).DeleteAsync(groupId);
        }

        var error = await Assert.ThrowsAsync<NotFoundException>(() => CreateAsync(droneId, groupId));

        Assert.Equal($"StudentGroup with Id {groupId} was not found.", error.Message);
        Assert.Equal(0, await CountLoansAsync());
        await using var verify = _db.CreateContext();
        var drone = Assert.Single(await new DroneService(verify).GetAllAsync());
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal(DroneStatus.Available, drone.Status);
    }
}
