using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-management: Create Drone, Drone Name is required, Drone Name is unique (REQ-001).</summary>
public class DroneServiceCreateTests : IDisposable
{
    private const string Description = "SSID: TELLO-9FA21B, right propeller guard damaged";

    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<DroneDto> CreateAsync(string? name, string? description = Description)
    {
        await using var context = _db.CreateContext();
        return await new DroneService(context).CreateAsync(new CreateDroneRequest(name, description));
    }

    // GIVEN no Drone named "TELLO-07" exists
    // WHEN a Drone with Name "TELLO-07" and Description "..." is created
    // THEN the Drone is stored with that Name and Description AND its status is "Available"
    [Fact]
    public async Task Drone_is_created()
    {
        var result = await CreateAsync("TELLO-07");

        Assert.Equal("TELLO-07", result.Name);
        Assert.Equal(Description, result.Description);
        Assert.Equal(DroneStatus.Available, result.Status);

        await using var verify = _db.CreateContext();
        var stored = await verify.Drones.SingleAsync();
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal("TELLO-07", stored.Name);
        Assert.Equal(Description, stored.Description);
        Assert.False(await verify.Loans.AnyAsync(l => l.DroneId == stored.Id && l.Status == LoanStatus.Active));
    }

    [Fact]
    public async Task Drone_is_created_with_trimmed_Name()
    {
        var result = await CreateAsync("  TELLO-07  ");

        Assert.Equal("TELLO-07", result.Name);
    }

    // GIVEN the Name is empty
    // WHEN a Drone is created
    // THEN the request is rejected as invalid AND no Drone is stored
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Name_is_required(string? name)
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(name));

        await using var verify = _db.CreateContext();
        Assert.Empty(await verify.Drones.ToListAsync());
    }

    // GIVEN a Drone named "TELLO-07" exists
    // WHEN another Drone with Name "TELLO-07" is created
    // THEN the request is rejected AND only one Drone named "TELLO-07" exists
    [Theory]
    [InlineData("TELLO-07")]
    [InlineData(" tello-07 ")]
    public async Task Duplicate_Name(string duplicate)
    {
        await CreateAsync("TELLO-07");

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(duplicate, "another drone"));

        await using var verify = _db.CreateContext();
        Assert.Equal(1, await verify.Drones.CountAsync(d => d.Name == "TELLO-07"));
    }

    // GIVEN the Drone "TELLO-07" was deleted and has a returned Loan
    // WHEN a new Drone with Name "TELLO-07" is created
    // THEN the new Drone is stored and listed as "Available"
    // AND the loan history still shows the returned Loan of the deleted "TELLO-07"
    [Fact]
    public async Task Name_of_a_deleted_Drone_is_reused()
    {
        var deleted = await CreateAsync("TELLO-07");
        await using (var context = _db.CreateContext())
        {
            var group = new StudentGroup { Name = "Group 3" };
            context.Loans.Add(new Loan
            {
                DroneId = deleted.Id,
                StudentGroup = group,
                LoanDate = new DateOnly(2026, 8, 18),
                ReturnDate = new DateOnly(2026, 8, 19),
                Status = LoanStatus.Returned
            });
            await context.SaveChangesAsync();
        }
        await using (var context = _db.CreateContext())
        {
            await new DroneService(context).DeleteAsync(deleted.Id);
        }

        var created = await CreateAsync("TELLO-07", "replacement");

        Assert.NotEqual(deleted.Id, created.Id);
        Assert.Equal(DroneStatus.Available, created.Status);
        await using var verify = _db.CreateContext();
        var listed = Assert.Single(await new DroneService(verify).GetAllAsync());
        Assert.Equal(created.Id, listed.Id);
        Assert.Equal("TELLO-07", listed.Name);
        Assert.Equal(DroneStatus.Available, listed.Status);
        var history = Assert.Single(await new LoanService(verify, _db.TimeProvider).GetHistoryAsync());
        Assert.Equal(deleted.Id, history.DroneId);
        Assert.Equal("TELLO-07", history.DroneName);
    }
}
