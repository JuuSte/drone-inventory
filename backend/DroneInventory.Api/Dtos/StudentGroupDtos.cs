namespace DroneInventory.Api.Dtos;

/// <summary>Request body for POST /api/student-groups.</summary>
public record CreateStudentGroupRequest(string? Name);

public record StudentGroupDto(int Id, string Name);
