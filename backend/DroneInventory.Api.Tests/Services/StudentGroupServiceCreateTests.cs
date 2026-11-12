using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Services;

/// <summary>student-group-management: Create StudentGroup (REQ-003), Name is required, Name is unique.</summary>
public class StudentGroupServiceCreateTests : IDisposable
{
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<StudentGroupDto> CreateAsync(string? name)
    {
        await using var context = _db.CreateContext();
        return await new StudentGroupService(context).CreateAsync(new CreateStudentGroupRequest(name));
    }

    // GIVEN no StudentGroup named "Group 3" exists
    // WHEN the StudentGroup "Group 3" is created
    // THEN the StudentGroup is stored
    [Fact]
    public async Task StudentGroup_is_created()
    {
        var result = await CreateAsync("Group 3");

        Assert.Equal("Group 3", result.Name);

        await using var verify = _db.CreateContext();
        var stored = await verify.StudentGroups.SingleAsync();
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal("Group 3", stored.Name);
    }

    [Fact]
    public async Task StudentGroup_is_created_with_trimmed_Name()
    {
        var result = await CreateAsync("  Group 3  ");

        Assert.Equal("Group 3", result.Name);
    }

    // GIVEN the Name is empty
    // WHEN a StudentGroup is created
    // THEN the request is rejected as invalid AND no StudentGroup is stored
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Name_is_required(string? name)
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(name));

        await using var verify = _db.CreateContext();
        Assert.Empty(await verify.StudentGroups.ToListAsync());
    }

    // GIVEN a StudentGroup named "Group 3" exists
    // WHEN another StudentGroup with Name "Group 3" is created
    // THEN the request is rejected AND only one StudentGroup named "Group 3" exists
    [Theory]
    [InlineData("Group 3")]
    [InlineData(" group 3 ")]
    public async Task Duplicate_Name(string duplicate)
    {
        await CreateAsync("Group 3");

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(duplicate));

        await using var verify = _db.CreateContext();
        Assert.Equal(1, await verify.StudentGroups.CountAsync(g => g.Name == "Group 3"));
    }

    // GIVEN the StudentGroup "Group 3" was deleted and has a returned Loan
    // WHEN a new StudentGroup with Name "Group 3" is created
    // THEN the new StudentGroup is stored and listed
    // AND the loan history still shows the returned Loan of the deleted "Group 3"
    [Fact]
    public async Task Name_of_a_deleted_StudentGroup_is_reused()
    {
        var deleted = await CreateAsync("Group 3");
        await using (var context = _db.CreateContext())
        {
            context.Loans.Add(new Loan
            {
                Drone = new Drone { Name = "TELLO-07", Description = "DJI Tello" },
                StudentGroupId = deleted.Id,
                LoanDate = new DateOnly(2026, 8, 18),
                ReturnDate = new DateOnly(2026, 8, 19),
                Status = LoanStatus.Returned
            });
            await context.SaveChangesAsync();
        }
        await using (var context = _db.CreateContext())
        {
            await new StudentGroupService(context).DeleteAsync(deleted.Id);
        }

        var created = await CreateAsync("Group 3");

        Assert.NotEqual(deleted.Id, created.Id);
        Assert.Equal("Group 3", created.Name);
        await using var verify = _db.CreateContext();
        var listed = Assert.Single(await new StudentGroupService(verify).GetAllAsync());
        Assert.Equal(created.Id, listed.Id);
        Assert.Equal("Group 3", listed.Name);
        var history = Assert.Single(await new LoanService(verify, _db.TimeProvider).GetHistoryAsync());
        Assert.Equal(deleted.Id, history.StudentGroupId);
        Assert.Equal("Group 3", history.StudentGroupName);
    }
}
