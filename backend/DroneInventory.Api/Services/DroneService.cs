using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class DroneService(AppDbContext context) : IDroneService
{
    public async Task<DroneDto> CreateAsync(CreateDroneRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException("Name is required.");
        }

        // Names are compared case-insensitively via the NOCASE collation of the Name column.
        // Only Drones that are not deleted count, so a deleted Name can be reused (design D5).
        if (await context.Drones.AnyAsync(d => d.Name == name && !d.IsDeleted, cancellationToken))
        {
            throw DuplicateName(name);
        }

        var drone = new Drone { Name = name, Description = request.Description?.Trim() ?? string.Empty };
        context.Drones.Add(drone);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request created the same Name between the check and the insert (unique index).
            throw DuplicateName(name);
        }

        // A newly created Drone has no Loan and is therefore available.
        return new DroneDto(drone.Id, drone.Name, drone.Description, DroneStatus.Available);
    }

    public async Task<IReadOnlyList<DroneDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Drones
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new DroneDto(
                d.Id,
                d.Name,
                d.Description,
                d.Loans.Any(l => l.Status == LoanStatus.Active) ? DroneStatus.Loaned : DroneStatus.Available))
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete as one conditional UPDATE: the active-Loan check and the write happen in the same
        // SQL statement, so no Loan can be created in between (design D3). Returned Loans do not block
        // the delete; they stay unchanged and keep showing the Drone Name in the loan history (REQ-011).
        var updated = await context.Drones
            .Where(d => d.Id == id && !d.IsDeleted && !d.Loans.Any(l => l.Status == LoanStatus.Active))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsDeleted, true), cancellationToken);
        if (updated == 1)
        {
            return;
        }

        // Nothing was updated: find out why to return the right error.
        var drone = await context.Drones
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"Drone with Id {id} was not found.");
        throw new ConflictException($"Drone \"{drone.Name}\" cannot be deleted because it has an active Loan.");
    }

    private static ConflictException DuplicateName(string name) =>
        new($"A Drone named \"{name}\" already exists.");
}
