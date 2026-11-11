using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-lending: Loan history (REQ-010).</summary>
public class LoanServiceHistoryTests : IDisposable
{
    // The fake TimeProvider starts at 19.08.2026 (SqliteTestDatabase.DefaultNow).
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<(int DroneId, int GroupId)> SeedTelloAndGroup3Async()
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = "TELLO-07", Description = "Tello" };
        var group = new StudentGroup { Name = "Group 3" };
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

    private async Task ReturnAsync(int id)
    {
        await using var context = _db.CreateContext();
        await new LoanService(context, _db.TimeProvider).ReturnAsync(id);
    }

    private async Task<IReadOnlyList<LoanDto>> GetHistoryAsync()
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).GetHistoryAsync();
    }

    // GIVEN the Loan of "TELLO-07" to "Group 3" was returned
    // WHEN the loan history is requested
    // THEN the Loan is shown with LoanDate, ReturnDate and accessories
    [Fact]
    public async Task Returned_Loan_appears_in_history()
    {
        var (droneId, groupId) = await SeedTelloAndGroup3Async();
        var loan = await LoanAsync(droneId, groupId, 4, 8);
        _db.TimeProvider.Advance(TimeSpan.FromDays(2));
        await ReturnAsync(loan.Id);

        var entry = Assert.Single(await GetHistoryAsync());

        Assert.Equal(loan.Id, entry.Id);
        Assert.Equal("TELLO-07", entry.DroneName);
        Assert.Equal("Group 3", entry.StudentGroupName);
        Assert.Equal(new DateOnly(2026, 8, 19), entry.LoanDate);
        Assert.Equal(new DateOnly(2026, 8, 21), entry.ReturnDate);
        Assert.Equal(4, entry.Batteries);
        Assert.Equal(8, entry.Propellers);
        Assert.Equal(nameof(LoanStatus.Returned), entry.Status);
    }

    // An active Loan is not part of the history.
    [Fact]
    public async Task Active_Loan_is_not_in_history()
    {
        var (droneId, groupId) = await SeedTelloAndGroup3Async();
        await LoanAsync(droneId, groupId, 1, 2);

        Assert.Empty(await GetHistoryAsync());
    }

    // GIVEN the Loan of "TELLO-07" to "Group 3" was returned
    // WHEN "TELLO-07" is loaned and returned again
    // THEN the loan history contains both Loans of "TELLO-07"
    [Fact]
    public async Task Loan_is_never_deleted()
    {
        var (droneId, groupId) = await SeedTelloAndGroup3Async();
        var first = await LoanAsync(droneId, groupId, 4, 8);
        _db.TimeProvider.Advance(TimeSpan.FromDays(1));
        await ReturnAsync(first.Id);

        _db.TimeProvider.Advance(TimeSpan.FromDays(1));
        var second = await LoanAsync(droneId, groupId, 2, 4);
        _db.TimeProvider.Advance(TimeSpan.FromDays(1));
        await ReturnAsync(second.Id);

        var history = await GetHistoryAsync();

        Assert.Equal(2, history.Count);
        Assert.All(history, l => Assert.Equal("TELLO-07", l.DroneName));
        // Most recent return first.
        Assert.Equal([second.Id, first.Id], history.Select(l => l.Id));
        Assert.Equal(new DateOnly(2026, 8, 20), history[1].ReturnDate);
        Assert.Equal(4, history[1].Batteries);
        Assert.Equal(new DateOnly(2026, 8, 22), history[0].ReturnDate);
        Assert.Equal(2, history[0].Batteries);
    }
}
