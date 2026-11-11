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

    public async Task<IReadOnlyList<StudentGroupDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.StudentGroups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new StudentGroupDto(g.Id, g.Name))
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await context.StudentGroups.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException($"StudentGroup with Id {id} was not found.");

        // Any Loan counts, active or returned: a returned Loan is loan history (REQ-010, design D1).
        if (await context.Loans.AnyAsync(l => l.StudentGroupId == id, cancellationToken))
        {
            throw HasLoans(group.Name);
        }

        context.StudentGroups.Remove(group);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request created a Loan between the check and the delete; the Restrict
            // foreign key makes the database refuse the delete (design D2).
            throw HasLoans(group.Name);
        }
    }

    private static ConflictException DuplicateName(string name) =>
        new($"A StudentGroup named \"{name}\" already exists.");

    private static ConflictException HasLoans(string name) =>
        new($"StudentGroup \"{name}\" cannot be deleted because it has Loans.");
}
