namespace DroneInventory.Api.Models;

public class Drone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // No availability column: availability is derived from the active Loan (design D1).
    public List<Loan> Loans { get; set; } = [];
}
