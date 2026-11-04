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
}
