using DroneInventory.Api.Dtos;

namespace DroneInventory.Api.Services;

public interface ILoanService
{
    /// <summary>
    /// Creates an active Loan with today's date as LoanDate and one Battery and one Propeller LoanItem.
    /// Throws ValidationException for a negative quantity.
    /// Throws NotFoundException if the Drone or the StudentGroup does not exist.
    /// Throws ConflictException if the Drone already has an active Loan.
    /// </summary>
    Task<LoanDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);
}
