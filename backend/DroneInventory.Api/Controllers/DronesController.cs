using DroneInventory.Api.Dtos;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/drones")]
public class DronesController(IDroneService droneService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<DroneDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DroneDto>> Create(CreateDroneRequest request, CancellationToken cancellationToken)
    {
        var drone = await droneService.CreateAsync(request, cancellationToken);
        return Created($"/api/drones/{drone.Id}", drone);
    }
}
