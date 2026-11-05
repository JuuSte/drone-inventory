using DroneInventory.Api.Dtos;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController(ILoanService loanService) : ControllerBase
{
    [HttpGet("active")]
    [ProducesResponseType<IReadOnlyList<LoanDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<LoanDto>> GetActive(CancellationToken cancellationToken) =>
        await loanService.GetActiveAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var loan = await loanService.CreateAsync(request, cancellationToken);
        return Created($"/api/loans/{loan.Id}", loan);
    }

    [HttpPost("{id:int}/return")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<LoanDto> Return(int id, CancellationToken cancellationToken) =>
        await loanService.ReturnAsync(id, cancellationToken);
}
