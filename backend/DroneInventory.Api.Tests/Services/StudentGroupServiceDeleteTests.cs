using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Services;

/// <summary>student-group-management: Delete StudentGroup (REQ-012), soft delete (design D3).</summary>
public class StudentGroupServiceDeleteTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 8, 19);

    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task DeleteAsync(int id)
    {
        await using var context = _db.CreateContext();
        await new StudentGroupService(context).DeleteAsync(id);
    }

    private async Task<IReadOnlyList<StudentGroupDto>> GetAllAsync()
    {
        await using var context = _db.CreateContext();
        return await new StudentGroupService(context).GetAllAsync();
    }

    private async Task<IReadOnlyList<LoanDto>> GetHistoryAsync()
    {
        await using var context = _db.CreateContext();
        return await new LoanService(context, _db.TimeProvider).GetHistoryAsync();
    }

    /// <summary>Stores a StudentGroup with one Loan per given status (each for its own Drone) and returns its Id.</summary>
    private async Task<int> SeedGroupAsync(string name, params LoanStatus[] loans)
    {
        await using var context = _db.CreateContext();
        var group = new StudentGroup { Name = name };
        var number = await context.Drones.CountAsync();
        foreach (var status in loans)
        {
            number++;
            group.Loans.Add(new Loan
            {
                Drone = new Drone { Name = $"TELLO-{number:00}", Description = "DJI Tello" },
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
        context.StudentGroups.Add(group);
        await context.SaveChangesAsync();
        return group.Id;
    }

    private async Task<StudentGroup?> FindStoredAsync(int id)
    {
        await using var context = _db.CreateContext();
        return await context.StudentGroups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == id);
    }

    private async Task<int> CountLoansAsync(int groupId)
    {
        await using var context = _db.CreateContext();
        return await context.Loans.CountAsync(l => l.StudentGroupId == groupId);
    }

    // GIVEN the StudentGroup "Group 3" has only returned Loans
    // WHEN "Group 3" is deleted
    // THEN "Group 3" is no longer listed
    // AND the loan history still shows the Loans of "Group 3" with the StudentGroup Name "Group 3"
    [Fact]
    public async Task StudentGroup_with_loan_history()
    {
        var id = await SeedGroupAsync("Group 3", LoanStatus.Returned, LoanStatus.Returned);
        await SeedGroupAsync("Group 5");

        await DeleteAsync(id);

        Assert.Equal("Group 5", Assert.Single(await GetAllAsync()).Name);
        var history = await GetHistoryAsync();
        Assert.Equal(2, history.Count);
        Assert.All(history, loan =>
        {
            Assert.Equal(id, loan.StudentGroupId);
            Assert.Equal("Group 3", loan.StudentGroupName);
            Assert.Equal(4, loan.Batteries);
            Assert.Equal(8, loan.Propellers);
        });
        Assert.Equal(["TELLO-01", "TELLO-02"], history.Select(l => l.DroneName).Order().ToArray());

        // Soft delete: the StudentGroup row and its Loans and LoanItems stay stored unchanged (REQ-010).
        var stored = await FindStoredAsync(id);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
        Assert.Equal(2, await CountLoansAsync(id));
        await using var verify = _db.CreateContext();
        Assert.Equal(4, await verify.LoanItems.CountAsync());
    }

    // GIVEN the StudentGroup "Group 5" has no Loans
    // WHEN "Group 5" is deleted
    // THEN "Group 5" is no longer listed
    [Fact]
    public async Task StudentGroup_without_Loans_is_deleted()
    {
        await SeedGroupAsync("Group 3", LoanStatus.Returned);
        var id = await SeedGroupAsync("Group 5");

        await DeleteAsync(id);

        Assert.Equal("Group 3", Assert.Single(await GetAllAsync()).Name);
        Assert.True((await FindStoredAsync(id))!.IsDeleted);
    }

    // GIVEN the StudentGroup "Group 3" has an active Loan
    // WHEN "Group 3" is deleted
    // THEN the request is rejected as a conflict AND the active Loan of "Group 3" is still listed
    [Fact]
    public async Task StudentGroup_with_an_active_Loan()
    {
        var id = await SeedGroupAsync("Group 3", LoanStatus.Returned, LoanStatus.Active);

        var error = await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(id));

        Assert.Equal("StudentGroup \"Group 3\" cannot be deleted because it has an active Loan.", error.Message);
        await using var context = _db.CreateContext();
        var active = Assert.Single(await new LoanService(context, _db.TimeProvider).GetActiveAsync());
        Assert.Equal("Group 3", active.StudentGroupName);
        Assert.Equal("Group 3", Assert.Single(await GetAllAsync()).Name);
        Assert.False((await FindStoredAsync(id))!.IsDeleted);
        Assert.Equal(2, await CountLoansAsync(id));
    }

    // GIVEN no StudentGroup with the requested Id exists
    // WHEN that StudentGroup is deleted
    // THEN the request is rejected as not found
    [Fact]
    public async Task Unknown_StudentGroup()
    {
        var id = await SeedGroupAsync("Group 3");

        var error = await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(id + 1));

        Assert.Equal($"StudentGroup with Id {id + 1} was not found.", error.Message);
        Assert.Single(await GetAllAsync());
    }

    // GIVEN the StudentGroup "Group 5" was deleted
    // WHEN "Group 5" is deleted again
    // THEN the request is rejected as not found
    [Fact]
    public async Task StudentGroup_is_deleted_twice()
    {
        var id = await SeedGroupAsync("Group 5");
        await DeleteAsync(id);

        var error = await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(id));

        Assert.Equal($"StudentGroup with Id {id} was not found.", error.Message);
        Assert.True((await FindStoredAsync(id))!.IsDeleted);
    }
}
