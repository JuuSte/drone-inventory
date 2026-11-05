using DroneInventory.Api.Data;
using DroneInventory.Api.Dtos;
using DroneInventory.Api.Exceptions;
using DroneInventory.Api.Models;
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

        var loan = new Loan
        {
            Drone = drone,
            StudentGroup = group,
            LoanDate = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime),
            Status = LoanStatus.Active
        };
        context.Loans.Add(loan);
        await context.SaveChangesAsync(cancellationToken);

        return ToDto(loan);
    }

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
