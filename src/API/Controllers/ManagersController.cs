using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Commands.ActivateManager;
using SkillsetsBackend.Application.Managers.Commands.AddManagerRole;
using SkillsetsBackend.Application.Managers.Commands.RemoveManagerRole;
using SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;
using SkillsetsBackend.Application.Managers.Commands.CreateManager;
using SkillsetsBackend.Application.Managers.Commands.DeactivateManager;
using SkillsetsBackend.Application.Managers.Commands.ProvisionManagerSkillport;
using SkillsetsBackend.Application.Managers.Commands.UpdateManager;
using SkillsetsBackend.Application.Managers.Queries.GetManagerById;
using SkillsetsBackend.Application.Managers.Queries.GetManagerCompanies;
using SkillsetsBackend.Application.Managers.Queries.GetManagerCredentials;
using SkillsetsBackend.Application.Managers.Queries.GetManagerRoles;
using SkillsetsBackend.Application.Managers.Queries.ListManagers;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/managers")]
[Authorize]
public sealed class ManagersController : ControllerBase
{
    private readonly ListManagersQueryHandler _listHandler;
    private readonly GetManagerByIdQueryHandler _getByIdHandler;
    private readonly GetManagerCompaniesQueryHandler _getCompaniesHandler;
    private readonly GetManagerRolesQueryHandler _getRolesHandler;
    private readonly GetManagerCredentialsQueryHandler _getCredentialsHandler;
    private readonly CreateManagerCommandHandler _createHandler;
    private readonly UpdateManagerCommandHandler _updateHandler;
    private readonly ChangeManagerPasswordCommandHandler _changePasswordHandler;
    private readonly ProvisionManagerSkillportCommandHandler _provisionSkillportHandler;
    private readonly DeactivateManagerCommandHandler _deactivateHandler;
    private readonly ActivateManagerCommandHandler _activateHandler;
    private readonly AddManagerRoleCommandHandler _addManagerRoleHandler;
    private readonly RemoveManagerRoleCommandHandler _removeManagerRoleHandler;

    public ManagersController(
        ListManagersQueryHandler listHandler,
        GetManagerByIdQueryHandler getByIdHandler,
        GetManagerCompaniesQueryHandler getCompaniesHandler,
        GetManagerRolesQueryHandler getRolesHandler,
        GetManagerCredentialsQueryHandler getCredentialsHandler,
        CreateManagerCommandHandler createHandler,
        UpdateManagerCommandHandler updateHandler,
        ChangeManagerPasswordCommandHandler changePasswordHandler,
        ProvisionManagerSkillportCommandHandler provisionSkillportHandler,
        DeactivateManagerCommandHandler deactivateHandler,
        ActivateManagerCommandHandler activateHandler,
        AddManagerRoleCommandHandler addManagerRoleHandler,
        RemoveManagerRoleCommandHandler removeManagerRoleHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _getCompaniesHandler = getCompaniesHandler;
        _getRolesHandler = getRolesHandler;
        _getCredentialsHandler = getCredentialsHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _changePasswordHandler = changePasswordHandler;
        _provisionSkillportHandler = provisionSkillportHandler;
        _deactivateHandler = deactivateHandler;
        _activateHandler = activateHandler;
        _addManagerRoleHandler = addManagerRoleHandler;
        _removeManagerRoleHandler = removeManagerRoleHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ManagerRequest request, CancellationToken cancellationToken)
    {
        var result = await _listHandler.Handle(
            request.Page,
            request.PageSize,
            request.Search,
            request.CompanyId,
            request.IsActive,
            request.SortBy,
            request.SortDescending,
            request.Role,
            GetCaller(),
            cancellationToken);

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
    public async Task<IActionResult> Create(CreateManagerCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(result.UserId, GetCaller(), cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.UserId, version = "1.0" },
            new
            {
                manager = created,
                skillportRequested = result.SkillportRequested,
                skillportProvisioned = result.SkillportProvisioned,
                skillportError = result.SkillportError,
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateManagerCommand command, CancellationToken cancellationToken)
    {
        await _updateHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, ChangeManagerPasswordCommand command, CancellationToken cancellationToken)
    {
        await _changePasswordHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/deactivate")]
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

    /// <summary>Grants a Manager role to an already-existing user (e.g. a current Employee) at the
    /// given company - SuperAdmin/CompanyAdmin only.</summary>
    [HttpPost("{id:int}/manager-role")]
    public async Task<IActionResult> AddManagerRole(int id, AddManagerRoleRequest request, CancellationToken cancellationToken)
    {
        await _addManagerRoleHandler.Handle(new AddManagerRoleCommand(id, request.CompanyId), GetCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>Revokes the Manager role from a user at the given company - SuperAdmin/CompanyAdmin
    /// only. Refused if it's the person's only active role anywhere.</summary>
    [HttpDelete("{id:int}/manager-role")]
    public async Task<IActionResult> RemoveManagerRole(int id, [FromQuery] int companyId, CancellationToken cancellationToken)
    {
        await _removeManagerRoleHandler.Handle(new RemoveManagerRoleCommand(id, companyId), GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/skillsoft")]
    public async Task<IActionResult> ProvisionSkillsoft(int id, ProvisionManagerSkillsoftRequest request, CancellationToken cancellationToken)
    {
        var command = new ProvisionManagerSkillportCommand(id, request.CompanyId, request.Password);
        var result = await _provisionSkillportHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(new { provisioned = result.Success, error = result.ErrorMessage });
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public sealed class ProvisionManagerSkillsoftRequest
{
    public int CompanyId { get; set; }

    public string Password { get; set; } = string.Empty;
}

public sealed class AddManagerRoleRequest
{
    public int CompanyId { get; set; }
}

public sealed class ManagerRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public int? CompanyId { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    /// <summary>Null for today's default (Manager/Admin) or "CompanyAdmin" to list company admins instead.</summary>
    public string? Role { get; set; }
}
