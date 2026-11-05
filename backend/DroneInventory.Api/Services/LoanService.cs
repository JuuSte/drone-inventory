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
        // Quantities must be integers >= 0 (REQ-006).
        if (request.Batteries < 0 || request.Propellers < 0)
        {
            throw new ValidationException("Quantities must be greater than or equal to 0.");
        }

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
            LoanDate = Today(),
            Status = LoanStatus.Active,
            Items =
            [
                new LoanItem { Type = LoanItemType.Battery, Quantity = request.Batteries },
                new LoanItem { Type = LoanItemType.Propeller, Quantity = request.Propellers }
            ]
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

    public async Task<IReadOnlyList<LoanDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await context.Loans
            .AsNoTracking()
            .Where(l => l.Status == LoanStatus.Active)
            .OrderBy(l => l.LoanDate)
            .ThenBy(l => l.Id)
            .Select(l => new LoanDto(
                l.Id,
                l.DroneId,
                l.Drone.Name,
                l.StudentGroupId,
                l.StudentGroup.Name,
                l.Items.Where(i => i.Type == LoanItemType.Battery).Sum(i => i.Quantity),
                l.Items.Where(i => i.Type == LoanItemType.Propeller).Sum(i => i.Quantity),
                l.LoanDate,
                l.ReturnDate,
                l.Status.ToString()))
            .ToListAsync(cancellationToken);

    public async Task<LoanDto> ReturnAsync(int id, CancellationToken cancellationToken = default)
    {
        var loan = await context.Loans
            .Include(l => l.Drone)
            .Include(l => l.StudentGroup)
            .Include(l => l.Items)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Loan with Id {id} was not found.");

        if (loan.Status == LoanStatus.Returned)
        {
            throw new ConflictException($"Loan with Id {id} is already returned.");
        }

        // The Drone becomes available again because it no longer has an active Loan (derived, not stored).
        loan.Status = LoanStatus.Returned;
        loan.ReturnDate = Today();
        await context.SaveChangesAsync(cancellationToken);

        return ToDto(loan);
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

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
