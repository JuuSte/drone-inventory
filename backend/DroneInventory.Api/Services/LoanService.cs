using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class LoanService(AppDbContext context, TimeProvider timeProvider) : ILoanService
{
    public async Task<LoanDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var drone = await context.Drones.FindAsync([request.DroneId], cancellationToken)
            ?? throw new NotFoundException($"Drone with Id {request.DroneId} was not found.");
        var group = await context.StudentGroups.FindAsync([request.StudentGroupId], cancellationToken)
            ?? throw new NotFoundException($"StudentGroup with Id {request.StudentGroupId} was not found.");

        // Service check gives a clear 409; the filtered unique index covers concurrent requests (design D2).
        if (await context.Loans.AnyAsync(l => l.DroneId == drone.Id && l.Status == LoanStatus.Active, cancellationToken))
        {
            throw AlreadyLoaned(drone.Name);
        }

        var loan = new Loan
        {
            Drone = drone,
            StudentGroup = group,
            LoanDate = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime),
            Status = LoanStatus.Active
        };
        context.Loans.Add(loan);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsActiveLoanIndexViolation(ex))
        {
            // A concurrent request loaned the same Drone between the check and the insert.
            throw AlreadyLoaned(drone.Name);
        }

        return ToDto(loan);
    }

    private static ConflictException AlreadyLoaned(string droneName) =>
        new($"The Drone \"{droneName}\" is already loaned.");

    // SQLite reports a violation of the filtered unique index as "UNIQUE constraint failed: Loans.DroneId".
    private static bool IsActiveLoanIndexViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintErrorCode } sqlite
        && sqlite.Message.Contains("Loans.DroneId", StringComparison.Ordinal);

    private const int SqliteConstraintErrorCode = 19;

    private static LoanDto ToDto(Loan loan) => new(
        loan.Id,
        loan.DroneId,
        loan.Drone.Name,
        loan.StudentGroupId,
        loan.StudentGroup.Name,
        Quantity(loan, LoanItemType.Battery),
        Quantity(loan, LoanItemType.Propeller),
        loan.LoanDate,
        loan.ReturnDate,
        loan.Status.ToString());

    private static int Quantity(Loan loan, LoanItemType type) =>
        loan.Items.Where(i => i.Type == type).Sum(i => i.Quantity);
}
