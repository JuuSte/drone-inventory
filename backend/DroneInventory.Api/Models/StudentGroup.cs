namespace DroneInventory.Api.Models;

public class StudentGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Loan> Loans { get; set; } = [];
}
