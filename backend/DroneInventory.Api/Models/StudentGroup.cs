namespace DroneInventory.Api.Models;

public class StudentGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Soft delete (design D1): a deleted StudentGroup is hidden from lists and new Loans but stays stored,
    // so its Loans keep showing its Name in the loan history. Filter with !IsDeleted explicitly;
    // do not add a global query filter (design D2).
    public bool IsDeleted { get; set; }

    public List<Loan> Loans { get; set; } = [];
}
