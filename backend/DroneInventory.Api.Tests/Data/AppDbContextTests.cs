using DroneInventory.Api.Data;
using DroneInventory.Api.Models;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

public class AppDbContextTests : IDisposable
{
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private static readonly DateOnly Today = new(2026, 8, 19);

    private async Task<(Drone Drone, StudentGroup First, StudentGroup Second)> SeedAsync()
    {
        await using var context = _db.CreateContext();
        var drone = new Drone { Name = "TELLO-07", Description = "DJI Tello" };
        var first = new StudentGroup { Name = "Group 3" };
        var second = new StudentGroup { Name = "Group 4" };
        context.AddRange(drone, first, second);
        await context.SaveChangesAsync();
        return (drone, first, second);
    }

    private static Loan ActiveLoan(int droneId, int groupId) =>
        new() { DroneId = droneId, StudentGroupId = groupId, LoanDate = Today, Status = LoanStatus.Active };

    // Scenario "Drone is already loaned" (REQ-005), database layer of design D2.
    [Fact]
    public async Task Second_active_Loan_for_the_same_Drone_is_refused_by_the_database()
    {
        var (drone, first, second) = await SeedAsync();

        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(ActiveLoan(drone.Id, first.Id));
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(ActiveLoan(drone.Id, second.Id));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.Contains("UNIQUE constraint failed: Loans.DroneId", exception.InnerException?.Message);
        }

        await using var verify = _db.CreateContext();
        Assert.Equal(1, await verify.Loans.CountAsync(l => l.DroneId == drone.Id && l.Status == LoanStatus.Active));
    }

    [Fact]
    public async Task Returned_Loans_do_not_block_a_new_active_Loan()
    {
        var (drone, first, second) = await SeedAsync();

        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(new Loan
            {
                DroneId = drone.Id, StudentGroupId = first.Id, LoanDate = Today,
                ReturnDate = Today, Status = LoanStatus.Returned
            });
            context.Loans.Add(ActiveLoan(drone.Id, second.Id));
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        Assert.Equal(2, await verify.Loans.CountAsync(l => l.DroneId == drone.Id));
    }

    [Fact]
    public async Task Drone_Name_is_unique_case_insensitively()
    {
        await SeedAsync();
        await using var context = _db.CreateContext();

        context.Drones.Add(new Drone { Name = "tello-07", Description = "" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task StudentGroup_Name_is_unique_case_insensitively()
    {
        await SeedAsync();
        await using var context = _db.CreateContext();

        context.StudentGroups.Add(new StudentGroup { Name = "GROUP 3" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    // Database layer of "Name of a deleted Drone is reused" (design D5): the unique Name index
    // only counts Drones that are not deleted.
    [Fact]
    public async Task Drone_Name_of_a_deleted_Drone_can_be_stored_again()
    {
        var (drone, _, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            (await context.Drones.SingleAsync(d => d.Id == drone.Id)).IsDeleted = true;
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            context.Drones.Add(new Drone { Name = "TELLO-07", Description = "replacement" });
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        Assert.Equal(2, await verify.Drones.CountAsync(d => d.Name == "TELLO-07"));
        Assert.Equal(1, await verify.Drones.CountAsync(d => d.Name == "TELLO-07" && !d.IsDeleted));
    }

    [Fact]
    public async Task Drone_Name_stays_unique_among_Drones_that_are_not_deleted()
    {
        var (drone, _, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            (await context.Drones.SingleAsync(d => d.Id == drone.Id)).IsDeleted = true;
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            context.Drones.Add(new Drone { Name = "TELLO-07", Description = "replacement" });
            await context.SaveChangesAsync();
        }

        await using var duplicate = _db.CreateContext();
        duplicate.Drones.Add(new Drone { Name = "tello-07", Description = "" });

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
    }

    // Database layer of "Name of a deleted StudentGroup is reused" (design D5).
    [Fact]
    public async Task StudentGroup_Name_of_a_deleted_StudentGroup_can_be_stored_again()
    {
        var (_, first, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            (await context.StudentGroups.SingleAsync(g => g.Id == first.Id)).IsDeleted = true;
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            context.StudentGroups.Add(new StudentGroup { Name = "Group 3" });
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        Assert.Equal(2, await verify.StudentGroups.CountAsync(g => g.Name == "Group 3"));
        Assert.Equal(1, await verify.StudentGroups.CountAsync(g => g.Name == "Group 3" && !g.IsDeleted));
    }

    [Fact]
    public async Task StudentGroup_Name_stays_unique_among_StudentGroups_that_are_not_deleted()
    {
        var (_, first, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            (await context.StudentGroups.SingleAsync(g => g.Id == first.Id)).IsDeleted = true;
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            context.StudentGroups.Add(new StudentGroup { Name = "Group 3" });
            await context.SaveChangesAsync();
        }

        await using var duplicate = _db.CreateContext();
        duplicate.StudentGroups.Add(new StudentGroup { Name = "GROUP 3" });

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
    }

    [Fact]
    public async Task Drone_with_Loans_cannot_be_deleted()
    {
        var (drone, first, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(ActiveLoan(drone.Id, first.Id));
            await context.SaveChangesAsync();
        }

        await using var delete = _db.CreateContext();
        delete.Drones.Remove(await delete.Drones.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => delete.SaveChangesAsync());
    }

    // Database layer of "StudentGroup with loan history" (REQ-012, design D2): the Restrict foreign key
    // refuses the delete even without the service check.
    [Fact]
    public async Task StudentGroup_with_Loans_cannot_be_deleted()
    {
        var (drone, first, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(ActiveLoan(drone.Id, first.Id));
            await context.SaveChangesAsync();
        }

        await using var delete = _db.CreateContext();
        delete.StudentGroups.Remove(await delete.StudentGroups.SingleAsync(g => g.Id == first.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => delete.SaveChangesAsync());
    }

    [Fact]
    public async Task Enums_are_stored_as_strings()
    {
        var (drone, first, _) = await SeedAsync();
        await using (var context = _db.CreateContext())
        {
            var loan = ActiveLoan(drone.Id, first.Id);
            loan.Items.Add(new LoanItem { Type = LoanItemType.Battery, Quantity = 4 });
            context.Loans.Add(loan);
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        var status = await verify.Database.SqlQueryRaw<string>("SELECT \"Status\" AS \"Value\" FROM \"Loans\"").SingleAsync();
        var type = await verify.Database.SqlQueryRaw<string>("SELECT \"Type\" AS \"Value\" FROM \"LoanItems\"").SingleAsync();
        Assert.Equal("Active", status);
        Assert.Equal("Battery", type);
    }
}
