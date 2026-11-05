namespace DroneInventory.Api.Dtos;

/// <summary>Request body for POST /api/loans.</summary>
public record CreateLoanRequest(int DroneId, int StudentGroupId);

/// <summary>A Loan with its Drone, StudentGroup and accessory quantities.</summary>
public record LoanDto(
    int Id,
    int DroneId,
    string DroneName,
    int StudentGroupId,
    string StudentGroupName,
    int Batteries,
    int Propellers,
    DateOnly LoanDate,
    DateOnly? ReturnDate,
    string Status);
