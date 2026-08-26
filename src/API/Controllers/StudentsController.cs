using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Commands.AddEmployeeRole;
using SkillsetsBackend.Application.Students.Commands.RemoveEmployeeRole;
using SkillsetsBackend.Application.Students.Commands.AssignStudentManager;
using SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;
using SkillsetsBackend.Application.Students.Commands.CreateStudent;
using SkillsetsBackend.Application.Students.Commands.ActivateStudent;
using SkillsetsBackend.Application.Students.Commands.DeactivateStudent;
using SkillsetsBackend.Application.Students.Commands.ProvisionStudentSkillport;
using SkillsetsBackend.Application.Students.Commands.UpdateStudent;
using SkillsetsBackend.Application.Students.Queries.GetStudentById;
using SkillsetsBackend.Application.Students.Queries.GetStudentCompanies;
using SkillsetsBackend.Application.Students.Queries.GetStudentCredentials;
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
    private readonly GetStudentCredentialsQueryHandler _getCredentialsHandler;
    private readonly CreateStudentCommandHandler _createHandler;
    private readonly UpdateStudentCommandHandler _updateHandler;
    private readonly ChangeStudentPasswordCommandHandler _changePasswordHandler;
    private readonly DeactivateStudentCommandHandler _deactivateHandler;
    private readonly ActivateStudentCommandHandler _activateHandler;
    private readonly ProvisionStudentSkillportCommandHandler _provisionSkillportHandler;
    private readonly AssignStudentManagerCommandHandler _assignManagerHandler;
    private readonly AddEmployeeRoleCommandHandler _addEmployeeRoleHandler;
    private readonly RemoveEmployeeRoleCommandHandler _removeEmployeeRoleHandler;

    public StudentsController(
        ListStudentsQueryHandler listHandler,
        GetStudentByIdQueryHandler getByIdHandler,
        GetStudentCompaniesQueryHandler getCompaniesHandler,
        GetStudentRolesQueryHandler getRolesHandler,
        GetStudentCredentialsQueryHandler getCredentialsHandler,
        CreateStudentCommandHandler createHandler,
        UpdateStudentCommandHandler updateHandler,
        ChangeStudentPasswordCommandHandler changePasswordHandler,
        DeactivateStudentCommandHandler deactivateHandler,
        ActivateStudentCommandHandler activateHandler,
        ProvisionStudentSkillportCommandHandler provisionSkillportHandler,
        AssignStudentManagerCommandHandler assignManagerHandler,
        AddEmployeeRoleCommandHandler addEmployeeRoleHandler,
        RemoveEmployeeRoleCommandHandler removeEmployeeRoleHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _getCompaniesHandler = getCompaniesHandler;
        _getRolesHandler = getRolesHandler;
        _getCredentialsHandler = getCredentialsHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _changePasswordHandler = changePasswordHandler;
        _deactivateHandler = deactivateHandler;
        _activateHandler = activateHandler;
        _provisionSkillportHandler = provisionSkillportHandler;
        _assignManagerHandler = assignManagerHandler;
        _addEmployeeRoleHandler = addEmployeeRoleHandler;
        _removeEmployeeRoleHandler = removeEmployeeRoleHandler;
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

    [HttpGet("{id:int}/credentials")]
    public async Task<IActionResult> GetCredentials(int id, CancellationToken cancellationToken)
    {
        var result = await _getCredentialsHandler.Handle(id, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(result.UserId, GetCaller(), cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.UserId, version = "1.0" },
            new
            {
                student = created,
                skillportRequested = result.SkillportRequested,
                skillportProvisioned = result.SkillportProvisioned,
                skillportError = result.SkillportError,
            });
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

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        await _activateHandler.Handle(id, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/manager")]
    public async Task<IActionResult> AssignManager(int id, AssignStudentManagerCommand command, CancellationToken cancellationToken)
    {
        await _assignManagerHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>Grants an Employee role to an already-existing user (e.g. a current Manager) at the
    /// given company - SuperAdmin/CompanyAdmin only.</summary>
    [HttpPost("{id:int}/employee-role")]
    public async Task<IActionResult> AddEmployeeRole(int id, AddEmployeeRoleRequest request, CancellationToken cancellationToken)
    {
        await _addEmployeeRoleHandler.Handle(new AddEmployeeRoleCommand(id, request.CompanyId), GetCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>Revokes the Employee role from a user at the given company - SuperAdmin/CompanyAdmin
    /// only. Refused if it's the person's only active role anywhere.</summary>
    [HttpDelete("{id:int}/employee-role")]
    public async Task<IActionResult> RemoveEmployeeRole(int id, [FromQuery] int companyId, CancellationToken cancellationToken)
    {
        await _removeEmployeeRoleHandler.Handle(new RemoveEmployeeRoleCommand(id, companyId), GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/skillsoft")]
    public async Task<IActionResult> ProvisionSkillsoft(int id, ProvisionStudentSkillsoftRequest request, CancellationToken cancellationToken)
    {
        var command = new ProvisionStudentSkillportCommand(id, request.CompanyId, request.Password);
        var result = await _provisionSkillportHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(new { provisioned = result.Success, error = result.ErrorMessage });
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public class ProvisionStudentSkillsoftRequest
{
    public int CompanyId { get; set; }

    public string Password { get; set; } = string.Empty;
}

public class AddEmployeeRoleRequest
{
    public int CompanyId { get; set; }
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
