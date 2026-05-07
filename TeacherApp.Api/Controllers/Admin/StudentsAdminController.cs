using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherApp.Api.Application.Admin.Students;
using TeacherApp.Api.Common;
using TeacherApp.Contracts.Admin;

namespace TeacherApp.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/students")]
[Authorize(Roles = Roles.Admin)]
public sealed class StudentsAdminController(IStudentsAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminStudentPerformanceResponse>>> List(CancellationToken cancellationToken)
        => Ok(await service.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminStudentDetailsResponse>> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var details = await service.GetDetailsAsync(id, cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("{id:guid}/answers")]
    public async Task<ActionResult<IReadOnlyList<AdminStudentAnswerResponse>>> Answers(
        [FromRoute] Guid id,
        [FromQuery] Guid? moduleId,
        [FromQuery] bool? isCorrect,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
        => Ok(await service.ListAnswersAsync(id, moduleId, isCorrect, take, cancellationToken));
}
