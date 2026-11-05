using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-lending: Loan accessories (REQ-006).</summary>
public class LoanServiceAccessoriesTests : IDisposable
{
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<(int DroneId, int StudentGroupId)> SeedAsync()
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = "TELLO-07", Description = "Tello" };
        var group = new StudentGroup { Name = "Group 3" };
        context.AddRange(drone, group);
        await context.SaveChangesAsync();
        return (drone.Id, group.Id);
    }

    private async Task<LoanDto> CreateAsync(CreateLoanRequest request)
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).CreateAsync(request);
    }

    // GIVEN "TELLO-07" is available and "Group 3" exists
    // WHEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
    // THEN the Loan contains a LoanItem Battery with Quantity 4
    // AND the Loan contains a LoanItem Propeller with Quantity 8
    [Fact]
    public async Task Loan_with_batteries_and_propellers()
    {
        var (droneId, groupId) = await SeedAsync();

        var loan = await CreateAsync(new CreateLoanRequest(droneId, groupId, Batteries: 4, Propellers: 8));

        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);

        await using var context = _db.CreateContext();
        var items = await context.LoanItems.Where(i => i.LoanId == loan.Id).ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.Equal(4, items.Single(i => i.Type == LoanItemType.Battery).Quantity);
        Assert.Equal(8, items.Single(i => i.Type == LoanItemType.Propeller).Quantity);
    }

    [Fact]
    public async Task Zero_quantities_are_allowed()
    {
        var (droneId, groupId) = await SeedAsync();

        var loan = await CreateAsync(new CreateLoanRequest(droneId, groupId, Batteries: 0, Propellers: 0));

        Assert.Equal(0, loan.Batteries);
        Assert.Equal(0, loan.Propellers);
        await using var context = _db.CreateContext();
        Assert.Equal(2, await context.LoanItems.CountAsync(i => i.LoanId == loan.Id));
    }

    // GIVEN "TELLO-07" is available and "Group 3" exists
    // WHEN a Loan is created with -1 batteries
    // THEN the Loan is rejected AND "TELLO-07" stays "Available"
    [Theory]
    [InlineData(-1, 8)]
    [InlineData(4, -1)]
    public async Task Negative_quantity(int batteries, int propellers)
    {
        var (droneId, groupId) = await SeedAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateAsync(new CreateLoanRequest(droneId, groupId, batteries, propellers)));

        await using var context = _db.CreateContext();
        Assert.Equal(0, await context.Loans.CountAsync());
        var drone = Assert.Single(await new DroneService(context).GetAllAsync());
        Assert.Equal(DroneStatus.Available, drone.Status);
    }
}
