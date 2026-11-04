using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Models;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Services;

/// <summary>student-group-management: List StudentGroups.</summary>
public class StudentGroupServiceGetAllTests : IDisposable
{
    private readonly SqliteTestDatabase<AppDbContext> _db = new(options => new AppDbContext(options));

    public void Dispose() => _db.Dispose();

    private async Task<IReadOnlyList<StudentGroupDto>> GetAllAsync()
    {
        await using var context = _db.CreateContext();
        return await new StudentGroupService(context).GetAllAsync();
    }

    private async Task SeedAsync(params string[] names)
    {
        await using var context = _db.CreateContext();
        context.StudentGroups.AddRange(names.Select(name => new StudentGroup { Name = name }));
        await context.SaveChangesAsync();
    }

    // GIVEN the StudentGroups "Group 1" and "Group 3" exist
    // WHEN the StudentGroup list is requested
    // THEN both "Group 1" and "Group 3" are shown
    [Fact]
    public async Task StudentGroups_are_listed()
    {
        await SeedAsync("Group 3", "Group 1");

        var groups = await GetAllAsync();

        Assert.Equal(["Group 1", "Group 3"], groups.Select(g => g.Name));
        Assert.All(groups, g => Assert.True(g.Id > 0));
    }

    [Fact]
    public async Task Empty_list_when_no_StudentGroups_exist()
    {
        Assert.Empty(await GetAllAsync());
    }
}
