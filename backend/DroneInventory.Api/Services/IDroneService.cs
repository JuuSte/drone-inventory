using DroneInventory.Api.Dtos;

namespace DroneInventory.Api.Services;

public interface IDroneService
{
    /// <summary>Creates a Drone. Throws ValidationException for an empty Name and ConflictException for a duplicate Name.</summary>
    Task<DroneDto> CreateAsync(CreateDroneRequest request, CancellationToken cancellationToken = default);
}
