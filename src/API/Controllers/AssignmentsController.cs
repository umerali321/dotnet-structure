using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Assignments.Commands.CancelAssignment;
using SkillsetsBackend.Application.Assignments.Commands.CreateAssignment;
using SkillsetsBackend.Application.Assignments.Commands.UpdateAssignment;
using SkillsetsBackend.Application.Assignments.Queries.ListMyAssignments;
using SkillsetsBackend.Application.Assignments.Queries.ListOngoingAssignments;
using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly CreateAssignmentCommandHandler _createHandler;
    private readonly UpdateAssignmentCommandHandler _updateHandler;
    private readonly CancelAssignmentCommandHandler _cancelHandler;
    private readonly ListOngoingAssignmentsQueryHandler _listOngoingHandler;
    private readonly ListMyAssignmentsQueryHandler _listMyHandler;

    public AssignmentsController(
        CreateAssignmentCommandHandler createHandler,
        UpdateAssignmentCommandHandler updateHandler,
        CancelAssignmentCommandHandler cancelHandler,
        ListOngoingAssignmentsQueryHandler listOngoingHandler,
        ListMyAssignmentsQueryHandler listMyHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _cancelHandler = cancelHandler;
        _listOngoingHandler = listOngoingHandler;
        _listMyHandler = listMyHandler;
    }

    /// <summary>Manager/CompanyAdmin "Ongoing Assignments" view - paginated, scoped to their
    /// managed companies (or all, for SuperAdmin).</summary>
    [HttpGet]
    public async Task<IActionResult> ListOngoing(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _listOngoingHandler.Handle(new ListOngoingAssignmentsQuery(page, pageSize, companyId), GetCaller(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Employee "My Assignments" view - always the caller's own assignments.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await _listMyHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _updateHandler.Handle(id, command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _cancelHandler.Handle(id, GetCaller(), cancellationToken);
        return NoContent();
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
