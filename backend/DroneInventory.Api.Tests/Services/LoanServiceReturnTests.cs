using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Services;

/// <summary>drone-lending: Return Loan (REQ-008) and Store return date (REQ-009).</summary>
public class LoanServiceReturnTests : IDisposable
{
    // The fake TimeProvider starts at 19.08.2026 (SqliteTestDatabase.DefaultNow).
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<LoanDto> LoanTelloToGroup3Async()
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = "TELLO-07", Description = "Tello" };
        var group = new StudentGroup { Name = "Group 3" };
        context.AddRange(drone, group);
        await context.SaveChangesAsync();
        return await new LoanService(context, _db.TimeProvider)
            .CreateAsync(new CreateLoanRequest(drone.Id, group.Id, 4, 8));
    }

    private async Task<LoanDto> ReturnAsync(int id)
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).ReturnAsync(id);
    }

    private async Task<Loan> FindLoanAsync(int id)
    {
        await using var context = _db.CreateContext();
        return (await context.Loans.FindAsync(id))!;
    }

    private async Task<string> DroneStatusAsync()
    {
        await using var context = _db.CreateContext();
        return Assert.Single(await new DroneService(context).GetAllAsync()).Status;
    }

    // GIVEN "TELLO-07" is loaned to "Group 3"
    // WHEN the Loan is returned
    // THEN the Loan is marked as "Returned" AND "TELLO-07" becomes "Available" again
    [Fact]
    public async Task Active_Loan_is_returned()
    {
        var loan = await LoanTelloToGroup3Async();
        Assert.Equal(DroneStatus.Loaned, await DroneStatusAsync());

        var returned = await ReturnAsync(loan.Id);

        Assert.Equal(nameof(LoanStatus.Returned), returned.Status);
        Assert.Equal("TELLO-07", returned.DroneName);
        Assert.Equal("Group 3", returned.StudentGroupName);
        Assert.Equal(4, returned.Batteries);
        Assert.Equal(8, returned.Propellers);
        Assert.Equal(LoanStatus.Returned, (await FindLoanAsync(loan.Id)).Status);
        Assert.Equal(DroneStatus.Available, await DroneStatusAsync());
    }

    // GIVEN the Loan is already returned
    // WHEN it is returned again
    // THEN the request is rejected AND the Loan's ReturnDate is unchanged
    [Fact]
    public async Task Return_twice()
    {
        var loan = await LoanTelloToGroup3Async();
        await ReturnAsync(loan.Id);
        _db.TimeProvider.Advance(TimeSpan.FromDays(3));

        await Assert.ThrowsAsync<ConflictException>(() => ReturnAsync(loan.Id));

        var stored = await FindLoanAsync(loan.Id);
        Assert.Equal(new DateOnly(2026, 8, 19), stored.ReturnDate);
        Assert.Equal(LoanStatus.Returned, stored.Status);
    }

    // GIVEN no Loan with the given Id exists
    // WHEN it is returned
    // THEN the request is rejected as not found
    [Fact]
    public async Task Unknown_Loan()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ReturnAsync(999));
    }

    // GIVEN an active Loan
    // WHEN the Loan is returned
    // THEN ReturnDate is set to the current date
    [Fact]
    public async Task ReturnDate_is_set()
    {
        var loan = await LoanTelloToGroup3Async();
        Assert.Null(loan.ReturnDate);
        _db.TimeProvider.Advance(TimeSpan.FromDays(2));
        var today = DateOnly.FromDateTime(_db.TimeProvider.GetLocalNow().DateTime);

        var returned = await ReturnAsync(loan.Id);

        Assert.Equal(today, returned.ReturnDate);
        Assert.Equal(today, (await FindLoanAsync(loan.Id)).ReturnDate);
        Assert.Equal(loan.LoanDate, returned.LoanDate);
    }
}
