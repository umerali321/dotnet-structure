using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;
using SkillsetsBackend.Application.Students.Commands.CreateStudent;
using SkillsetsBackend.Application.Students.Commands.DeactivateStudent;
using SkillsetsBackend.Application.Students.Commands.UpdateStudent;
using SkillsetsBackend.Application.Students.Queries.GetStudentById;
using SkillsetsBackend.Application.Students.Queries.GetStudentCompanies;
using SkillsetsBackend.Application.Students.Queries.GetStudentRoles;
using SkillsetsBackend.Application.Students.Queries.ListStudents;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly ListStudentsQueryHandler _listHandler;
    private readonly GetStudentByIdQueryHandler _getByIdHandler;
    private readonly GetStudentCompaniesQueryHandler _getCompaniesHandler;
    private readonly GetStudentRolesQueryHandler _getRolesHandler;
    private readonly CreateStudentCommandHandler _createHandler;
    private readonly UpdateStudentCommandHandler _updateHandler;
    private readonly ChangeStudentPasswordCommandHandler _changePasswordHandler;
    private readonly DeactivateStudentCommandHandler _deactivateHandler;

    public StudentsController(
        ListStudentsQueryHandler listHandler,
        GetStudentByIdQueryHandler getByIdHandler,
        GetStudentCompaniesQueryHandler getCompaniesHandler,
        GetStudentRolesQueryHandler getRolesHandler,
        CreateStudentCommandHandler createHandler,
        UpdateStudentCommandHandler updateHandler,
        ChangeStudentPasswordCommandHandler changePasswordHandler,
        DeactivateStudentCommandHandler deactivateHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _getCompaniesHandler = getCompaniesHandler;
        _getRolesHandler = getRolesHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _changePasswordHandler = changePasswordHandler;
        _deactivateHandler = deactivateHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListStudentsRequest request, CancellationToken cancellationToken)
    {
        var query = new ListStudentsQuery(
            request.Page, request.PageSize, request.Search, request.CompanyId,
            request.StudentType, request.IsActive, request.SortBy, request.SortDescending);

        var result = await _listHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(id, GetCaller(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/companies")]
    public async Task<IActionResult> GetCompanies(int id, CancellationToken cancellationToken)
    {
        var result = await _getCompaniesHandler.Handle(id, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/roles")]
    public async Task<IActionResult> GetRoles(int id, CancellationToken cancellationToken)
    {
        var result = await _getRolesHandler.Handle(id, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        var newUserId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(newUserId, GetCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = newUserId, version = "1.0" }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateStudentCommand command, CancellationToken cancellationToken)
    {
        await _updateHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, ChangeStudentPasswordCommand command, CancellationToken cancellationToken)
    {
        await _changePasswordHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _deactivateHandler.Handle(id, GetCaller(), cancellationToken);
        return NoContent();
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public class ListStudentsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? Search { get; set; }

    public int? CompanyId { get; set; }

    public string? StudentType { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
