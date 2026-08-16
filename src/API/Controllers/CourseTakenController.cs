using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Commands.MarkCourseTakenComplete;
using SkillsetsBackend.Application.CourseLibrary.Commands.TakeCourse;
using SkillsetsBackend.Application.CourseLibrary.Queries.ListCourseTaken;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/course-taken")]
[Authorize]
public class CourseTakenController : ControllerBase
{
    private readonly TakeCourseCommandHandler _takeCourseHandler;
    private readonly MarkCourseTakenCompleteCommandHandler _markCompleteHandler;
    private readonly ListCourseTakenQueryHandler _listHandler;

    public CourseTakenController(
        TakeCourseCommandHandler takeCourseHandler,
        MarkCourseTakenCompleteCommandHandler markCompleteHandler,
        ListCourseTakenQueryHandler listHandler)
    {
        _takeCourseHandler = takeCourseHandler;
        _markCompleteHandler = markCompleteHandler;
        _listHandler = listHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.Handle(new ListCourseTakenQuery(page, pageSize), GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> TakeCourse(TakeCourseCommand command, CancellationToken cancellationToken)
    {
        var result = await _takeCourseHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> MarkComplete(int id, CancellationToken cancellationToken)
    {
        await _markCompleteHandler.Handle(new MarkCourseTakenCompleteCommand(id), GetCaller(), cancellationToken);
        return NoContent();
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
