using System.Globalization;
using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-lending: List active Loans (REQ-007).</summary>
public class LoanServiceGetActiveTests : IDisposable
{
    // The fake TimeProvider is fixed to 19.08.2026 (SqliteTestDatabase.DefaultNow).
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<(int DroneId, int StudentGroupId)> SeedAsync(string droneName = "TELLO-07", string groupName = "Group 3")
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = droneName, Description = "Tello" };
        var group = new StudentGroup { Name = groupName };
        context.AddRange(drone, group);
        await context.SaveChangesAsync();
        return (drone.Id, group.Id);
    }

    private async Task<LoanDto> LoanAsync(int droneId, int groupId, int batteries, int propellers)
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider)
            .CreateAsync(new CreateLoanRequest(droneId, groupId, batteries, propellers));
    }

    private async Task<IReadOnlyList<LoanDto>> GetActiveAsync()
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).GetActiveAsync();
    }

    private static string Row(LoanDto loan) =>
        $"{loan.StudentGroupName} | {loan.DroneName} | {loan.Batteries} | {loan.Propellers} | " +
        loan.LoanDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    // GIVEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers on 19.08.2026
    // WHEN the active Loans are requested
    // THEN the row "Group 3 | TELLO-07 | 4 | 8 | 19.08.2026" is shown
    [Fact]
    public async Task Active_Loan_is_listed()
    {
        var (droneId, groupId) = await SeedAsync();
        await LoanAsync(droneId, groupId, batteries: 4, propellers: 8);

        var loan = Assert.Single(await GetActiveAsync());

        Assert.Equal("Group 3 | TELLO-07 | 4 | 8 | 19.08.2026", Row(loan));
        Assert.Equal(new DateOnly(2026, 8, 19), loan.LoanDate);
        Assert.Equal(nameof(LoanStatus.Active), loan.Status);
    }

    // GIVEN the Loan of "TELLO-07" to "Group 3" was returned
    // WHEN the active Loans are requested
    // THEN that Loan is not shown
    [Fact]
    public async Task Returned_Loan_is_not_listed_as_active()
    {
        var (droneId, groupId) = await SeedAsync();
        var returned = await LoanAsync(droneId, groupId, batteries: 4, propellers: 8);
        await using (var context = _db.CreateContext())
        {
            var loan = await context.Loans.FindAsync(returned.Id);
            loan!.Status = LoanStatus.Returned;
            loan.ReturnDate = new DateOnly(2026, 8, 20);
            await context.SaveChangesAsync();
        }

        Assert.DoesNotContain(await GetActiveAsync(), l => l.Id == returned.Id);
    }

    [Fact]
    public async Task Only_active_Loans_are_listed_oldest_first()
    {
        var (firstDrone, firstGroup) = await SeedAsync("TELLO-01", "Group 1");
        var (secondDrone, secondGroup) = await SeedAsync("TELLO-02", "Group 2");
        var (thirdDrone, thirdGroup) = await SeedAsync("TELLO-03", "Group 3");

        // Inserted first (lower Id) but with a later LoanDate than the Loan created below on 19.08.2026.
        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(new Loan
            {
                DroneId = firstDrone, StudentGroupId = firstGroup,
                LoanDate = new DateOnly(2026, 8, 21), Status = LoanStatus.Active
            });
            await context.SaveChangesAsync();
        }
        await LoanAsync(secondDrone, secondGroup, 3, 4);
        var returned = await LoanAsync(thirdDrone, thirdGroup, 0, 0);
        await using (var context = _db.CreateContext())
        {
            (await context.Loans.FindAsync(returned.Id))!.Status = LoanStatus.Returned;
            await context.SaveChangesAsync();
        }

        var loans = await GetActiveAsync();

        Assert.Equal(["TELLO-02", "TELLO-01"], loans.Select(l => l.DroneName));
    }

    [Fact]
    public async Task Empty_list_when_no_Loans_exist()
    {
        Assert.Empty(await GetActiveAsync());
    }
}
