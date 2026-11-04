using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class StudentGroupService(AppDbContext context) : IStudentGroupService
{
    public async Task<StudentGroupDto> CreateAsync(CreateStudentGroupRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException("Name is required.");
        }

        // Names are compared case-insensitively via the NOCASE collation of the Name column.
        if (await context.StudentGroups.AnyAsync(g => g.Name == name, cancellationToken))
        {
            throw DuplicateName(name);
        }

        var group = new StudentGroup { Name = name };
        context.StudentGroups.Add(group);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request created the same Name between the check and the insert (unique index).
            throw DuplicateName(name);
        }

        return new StudentGroupDto(group.Id, group.Name);
    }

    private static ConflictException DuplicateName(string name) =>
        new($"A StudentGroup named \"{name}\" already exists.");
}
