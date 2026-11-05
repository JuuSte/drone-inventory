using DroneInventory.Api.Dtos;

namespace DroneInventory.Api.Services;

public interface ILoanService
{
    /// <summary>
    /// Creates an active Loan with today's date as LoanDate.
    /// Throws NotFoundException if the Drone or the StudentGroup does not exist.
    /// </summary>
    Task<LoanDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);
}
