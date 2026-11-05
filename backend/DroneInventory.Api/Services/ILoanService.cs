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

    /// <summary>Lists all Loans with Status Active, oldest LoanDate first. Returned Loans are not included.</summary>
    Task<IReadOnlyList<LoanDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an active Loan: sets Status Returned and today's date as ReturnDate.
    /// Throws NotFoundException for an unknown Loan and ConflictException if the Loan is already returned.
    /// </summary>
    Task<LoanDto> ReturnAsync(int id, CancellationToken cancellationToken = default);
}
