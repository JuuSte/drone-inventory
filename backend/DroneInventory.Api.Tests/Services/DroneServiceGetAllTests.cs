using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-management: List Drones (REQ-002).</summary>
public class DroneServiceGetAllTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 8, 19);

    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<IReadOnlyList<DroneDto>> GetAllAsync()
    {
        await using var context = _db.CreateContext();
        return await new DroneService(context).GetAllAsync();
    }

    private async Task SeedAsync(params (string Name, LoanStatus[] Loans)[] drones)
    {
        await using var context = _db.CreateContext();
        var group = new StudentGroup { Name = "Group 3" };
        foreach (var (name, loans) in drones)
        {
            var drone = new Drone { Name = name, Description = $"{name} description" };
            foreach (var status in loans)
            {
                drone.Loans.Add(new Loan
                {
                    StudentGroup = group,
                    LoanDate = Today,
                    ReturnDate = status == LoanStatus.Returned ? Today : null,
                    Status = status
                });
            }
            context.Drones.Add(drone);
        }
        await context.SaveChangesAsync();
    }

    // GIVEN "TELLO-02" has an active Loan and "TELLO-07" has none
    // WHEN the Drone list is requested
    // THEN "TELLO-02" is shown as "Loaned" AND "TELLO-07" is shown as "Available"
    [Fact]
    public async Task Status_derived_from_active_Loan()
    {
        await SeedAsync(("TELLO-02", [LoanStatus.Active]), ("TELLO-07", []));

        var drones = await GetAllAsync();

        Assert.Equal(DroneStatus.Loaned, drones.Single(d => d.Name == "TELLO-02").Status);
        Assert.Equal(DroneStatus.Available, drones.Single(d => d.Name == "TELLO-07").Status);
    }

    // GIVEN "TELLO-07" has a Loan with Status "Returned" and no active Loan
    // WHEN the Drone list is requested
    // THEN "TELLO-07" is shown as "Available"
    [Fact]
    public async Task Drone_with_only_returned_Loans_is_available()
    {
        await SeedAsync(("TELLO-07", [LoanStatus.Returned, LoanStatus.Returned]));

        var drone = Assert.Single(await GetAllAsync());

        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal(DroneStatus.Available, drone.Status);
    }

    [Fact]
    public async Task Drone_with_returned_and_active_Loan_is_loaned()
    {
        await SeedAsync(("TELLO-07", [LoanStatus.Returned, LoanStatus.Active]));

        Assert.Equal(DroneStatus.Loaned, Assert.Single(await GetAllAsync()).Status);
    }

    [Fact]
    public async Task Drones_are_listed_with_Name_and_Description_ordered_by_Name()
    {
        await SeedAsync(("TELLO-07", []), ("TELLO-02", []));

        var drones = await GetAllAsync();

        Assert.Equal(["TELLO-02", "TELLO-07"], drones.Select(d => d.Name));
        Assert.Equal("TELLO-02 description", drones[0].Description);
    }

    [Fact]
    public async Task Empty_list_when_no_Drones_exist()
    {
        Assert.Empty(await GetAllAsync());
    }
}
