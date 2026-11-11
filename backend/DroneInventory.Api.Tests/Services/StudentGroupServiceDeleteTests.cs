using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DroneInventory.Api.Tests.Services;

/// <summary>student-group-management: Delete StudentGroup (REQ-012).</summary>
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
                Items = [new LoanItem { Type = LoanItemType.Propeller, Quantity = 8 }]
            });
        }
        context.StudentGroups.Add(group);
        await context.SaveChangesAsync();
        return group.Id;
    }

    private async Task<int> CountLoansAsync(int groupId)
    {
        await using var context = _db.CreateContext();
        return await context.Loans.CountAsync(l => l.StudentGroupId == groupId);
    }

    // GIVEN the StudentGroup "Group 5" has no Loans
    // WHEN "Group 5" is deleted
    // THEN the StudentGroup is removed AND "Group 5" is no longer listed
    [Fact]
    public async Task StudentGroup_without_Loans_is_deleted()
    {
        await SeedGroupAsync("Group 3", LoanStatus.Returned);
        var id = await SeedGroupAsync("Group 5");

        await DeleteAsync(id);

        await using (var verify = _db.CreateContext())
        {
            Assert.Null(await verify.StudentGroups.FindAsync(id));
        }
        var remaining = Assert.Single(await GetAllAsync());
        Assert.Equal("Group 3", remaining.Name);
    }

    // GIVEN the StudentGroup "Group 3" has a Loan with Status "Returned"
    // WHEN "Group 3" is deleted
    // THEN the request is rejected as a conflict AND the StudentGroup "Group 3" and its Loans remain stored
    [Fact]
    public async Task StudentGroup_with_loan_history()
    {
        var id = await SeedGroupAsync("Group 3", LoanStatus.Returned);

        var error = await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(id));

        Assert.Equal("StudentGroup \"Group 3\" cannot be deleted because it has Loans.", error.Message);
        Assert.Equal("Group 3", Assert.Single(await GetAllAsync()).Name);
        Assert.Equal(1, await CountLoansAsync(id));
        await using var verify = _db.CreateContext();
        Assert.Equal(1, await verify.LoanItems.CountAsync());
    }

    // GIVEN the StudentGroup "Group 3" has an active Loan
    // WHEN "Group 3" is deleted
    // THEN the request is rejected as a conflict AND the active Loan of "Group 3" is still listed
    [Fact]
    public async Task StudentGroup_with_an_active_Loan()
    {
        var id = await SeedGroupAsync("Group 3", LoanStatus.Active);

        await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(id));

        await using var context = _db.CreateContext();
        var active = Assert.Single(await new LoanService(context, _db.TimeProvider).GetActiveAsync());
        Assert.Equal("Group 3", active.StudentGroupName);
        Assert.Equal("Group 3", Assert.Single(await GetAllAsync()).Name);
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

    // Same as "StudentGroup with an active Loan", but the Loan is created after the service check
    // (concurrent request): the database refuses the delete and it is mapped to the same conflict (design D2).
    [Fact]
    public async Task StudentGroup_with_a_concurrent_Loan_is_not_deleted()
    {
        var id = await SeedGroupAsync("Group 5");
        int droneId;
        await using (var context = _db.CreateContext())
        {
            var drone = new Drone { Name = "TELLO-08", Description = "DJI Tello" };
            context.Drones.Add(drone);
            await context.SaveChangesAsync();
            droneId = drone.Id;
        }
        var interceptor = new InsertCompetingLoanInterceptor(droneId, id);

        await using (var context = _db.CreateContext(interceptor))
        {
            await Assert.ThrowsAsync<ConflictException>(() => new StudentGroupService(context).DeleteAsync(id));
        }

        Assert.True(interceptor.Inserted);
        Assert.Equal("Group 5", Assert.Single(await GetAllAsync()).Name);
        Assert.Equal(1, await CountLoansAsync(id));
    }

    /// <summary>Simulates a concurrent request by inserting an active Loan right before the service saves the delete.</summary>
    private sealed class InsertCompetingLoanInterceptor(int droneId, int groupId) : SaveChangesInterceptor
    {
        public bool Inserted { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Inserted)
            {
                Inserted = true;
                await eventData.Context!.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"Loans\" (\"DroneId\", \"StudentGroupId\", \"LoanDate\", \"Status\") VALUES ({droneId}, {groupId}, '2026-08-19', 'Active')",
                    cancellationToken);
            }
            return result;
        }
    }
}
