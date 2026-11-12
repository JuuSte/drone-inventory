namespace DroneInventory.Api.Models;

public class Drone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Soft delete (design D1): a deleted Drone is hidden from lists and new Loans but stays stored,
    // so its Loans keep showing its Name in the loan history. Filter with !IsDeleted explicitly;
    // do not add a global query filter (design D2).
    public bool IsDeleted { get; set; }

    // No availability column: availability is derived from the active Loan (design D1).
    public List<Loan> Loans { get; set; } = [];
}
