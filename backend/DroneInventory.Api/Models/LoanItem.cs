namespace DroneInventory.Api.Models;

public class LoanItem
{
    public int Id { get; set; }

    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public LoanItemType Type { get; set; }
    public int Quantity { get; set; }
}
