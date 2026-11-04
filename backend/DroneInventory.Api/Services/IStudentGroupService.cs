using DroneInventory.Api.Dtos;

namespace DroneInventory.Api.Services;

public interface IStudentGroupService
{
    /// <summary>Creates a StudentGroup. Throws ValidationException for an empty Name and ConflictException for a duplicate Name.</summary>
    Task<StudentGroupDto> CreateAsync(CreateStudentGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists all StudentGroups ordered by Name.</summary>
    Task<IReadOnlyList<StudentGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
