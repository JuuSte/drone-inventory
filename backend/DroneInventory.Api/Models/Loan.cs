namespace DroneInventory.Api.Models;

public class Loan
{
    public int Id { get; set; }

    public int DroneId { get; set; }
    public Drone Drone { get; set; } = null!;

    public int StudentGroupId { get; set; }
    public StudentGroup StudentGroup { get; set; } = null!;

    public DateOnly LoanDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;

    public List<LoanItem> Items { get; set; } = [];
}
