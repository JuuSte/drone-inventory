using DroneInventory.Api.Dtos;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/student-groups")]
public class StudentGroupsController(IStudentGroupService studentGroupService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<StudentGroupDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentGroupDto>> Create(CreateStudentGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await studentGroupService.CreateAsync(request, cancellationToken);
        return Created($"/api/student-groups/{group.Id}", group);
    }
}
