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
        if (await context.Drones.AnyAsync(d => d.Name == name, cancellationToken))
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
            .OrderBy(d => d.Name)
            .Select(d => new DroneDto(
                d.Id,
                d.Name,
                d.Description,
                d.Loans.Any(l => l.Status == LoanStatus.Active) ? DroneStatus.Loaned : DroneStatus.Available))
            .ToListAsync(cancellationToken);

    private static ConflictException DuplicateName(string name) =>
        new($"A Drone named \"{name}\" already exists.");
}
