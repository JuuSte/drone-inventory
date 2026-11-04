namespace DroneInventory.Api.Dtos;

/// <summary>Request body for POST /api/drones.</summary>
public record CreateDroneRequest(string? Name, string? Description);

/// <summary>A Drone with its derived status ("Available" or "Loaned").</summary>
public record DroneDto(int Id, string Name, string Description, string Status);

public static class DroneStatus
{
    public const string Available = "Available";
    public const string Loaned = "Loaned";
}
