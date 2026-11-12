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
        // Only StudentGroups that are not deleted count, so a deleted Name can be reused (design D5).
        if (await context.StudentGroups.AnyAsync(g => g.Name == name && !g.IsDeleted, cancellationToken))
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
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Name)
            .Select(g => new StudentGroupDto(g.Id, g.Name))
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete as one conditional UPDATE: the active-Loan check and the write happen in the same
        // SQL statement, so no Loan can be created in between (design D3). Returned Loans do not block
        // the delete; they stay unchanged and keep showing the StudentGroup Name in the loan history (REQ-012).
        var updated = await context.StudentGroups
            .Where(g => g.Id == id && !g.IsDeleted && !g.Loans.Any(l => l.Status == LoanStatus.Active))
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.IsDeleted, true), cancellationToken);
        if (updated == 1)
        {
            return;
        }

        // Nothing was updated: find out why to return the right error.
        var group = await context.StudentGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"StudentGroup with Id {id} was not found.");
        throw new ConflictException($"StudentGroup \"{group.Name}\" cannot be deleted because it has an active Loan.");
    }

    private static ConflictException DuplicateName(string name) =>
        new($"A StudentGroup named \"{name}\" already exists.");
}
