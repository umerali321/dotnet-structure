using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.Commands.CreateRole;
using SkillsetsBackend.Application.RoleManagement.Commands.UpdateRolePermissions;
using SkillsetsBackend.Application.RoleManagement.Queries.GetMyPermissions;
using SkillsetsBackend.Application.RoleManagement.Queries.GetRoleById;
using SkillsetsBackend.Application.RoleManagement.Queries.GetUserEffectivePermissions;
using SkillsetsBackend.Application.RoleManagement.Queries.ListPermissions;
using SkillsetsBackend.Application.RoleManagement.Queries.ListRoles;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly ListPermissionsQueryHandler _listPermissionsHandler;
    private readonly ListRolesQueryHandler _listRolesHandler;
    private readonly GetRoleByIdQueryHandler _getRoleByIdHandler;
    private readonly GetMyPermissionsQueryHandler _getMyPermissionsHandler;
    private readonly GetUserEffectivePermissionsQueryHandler _getUserPermissionsHandler;
    private readonly CreateRoleCommandHandler _createRoleHandler;
    private readonly UpdateRolePermissionsCommandHandler _updateRolePermissionsHandler;

    public RolesController(
        ListPermissionsQueryHandler listPermissionsHandler,
        ListRolesQueryHandler listRolesHandler,
        GetRoleByIdQueryHandler getRoleByIdHandler,
        GetMyPermissionsQueryHandler getMyPermissionsHandler,
        GetUserEffectivePermissionsQueryHandler getUserPermissionsHandler,
        CreateRoleCommandHandler createRoleHandler,
        UpdateRolePermissionsCommandHandler updateRolePermissionsHandler)
    {
        _listPermissionsHandler = listPermissionsHandler;
        _listRolesHandler = listRolesHandler;
        _getRoleByIdHandler = getRoleByIdHandler;
        _getMyPermissionsHandler = getMyPermissionsHandler;
        _getUserPermissionsHandler = getUserPermissionsHandler;
        _createRoleHandler = createRoleHandler;
        _updateRolePermissionsHandler = updateRolePermissionsHandler;
    }

    /// <summary>Read-only catalog - Permissions are never created through the API, only seeded via migration.</summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> ListPermissions(CancellationToken cancellationToken)
    {
        var result = await _listPermissionsHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/my-permissions")]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var result = await _getMyPermissionsHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var result = await _listRolesHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{id}")]
    public async Task<IActionResult> GetById(byte id, CancellationToken cancellationToken)
    {
        var result = await _getRoleByIdHandler.Handle(id, GetCaller(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>SuperAdmin only.</summary>
    [HttpPost("roles")]
    public async Task<IActionResult> Create(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var roleId = await _createRoleHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getRoleByIdHandler.Handle(roleId, GetCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = roleId, version = "1.0" }, created);
    }

    /// <summary>SuperAdmin only - rejects the 5 seeded system roles.</summary>
    [HttpPut("roles/{id}/permissions")]
    public async Task<IActionResult> UpdatePermissions(byte id, UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        await _updateRolePermissionsHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    /// <summary>SuperAdmin: any user. CompanyAdmin: only users in a company they manage.</summary>
    [HttpGet("users/{userId:int}/permissions")]
    public async Task<IActionResult> GetUserPermissions(int userId, CancellationToken cancellationToken)
    {
        var result = await _getUserPermissionsHandler.Handle(userId, GetCaller(), cancellationToken);
        return Ok(result);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
