using DroneInventory.Api.Dtos;

namespace DroneInventory.Api.Services;

public interface IDroneService
{
    /// <summary>Creates a Drone. Throws ValidationException for an empty Name and ConflictException for a duplicate Name.</summary>
    Task<DroneDto> CreateAsync(CreateDroneRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists all Drones ordered by Name; the status is derived from the active Loan, never stored.</summary>
    Task<IReadOnlyList<DroneDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Drone that has no Loans (REQ-011). Throws NotFoundException for an unknown Id and
    /// ConflictException if any Loan, active or returned, references the Drone, so loan history is never lost.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
