using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SystemAdmins.Commands.CreateSystemAdmin;
using SkillsetsBackend.Application.SystemAdmins.Commands.ResetSystemAdminPassword;
using SkillsetsBackend.Application.SystemAdmins.Queries.ListSystemAdmins;

namespace SkillsetsBackend.API.Controllers;

/// <summary>
/// SuperAdmin-only management of System Administrators. Every action re-checks that the caller is a
/// SuperAdmin in the handler itself (SystemAdminAuthorization) - the check is not delegated to a
/// grantable permission, so a SystemAdmin can never be given the ability to create peers or widen
/// its own reach.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system-admins")]
[Authorize]
public sealed class SystemAdminsController : ControllerBase
{
    private readonly ListSystemAdminsQueryHandler _listHandler;
    private readonly CreateSystemAdminCommandHandler _createHandler;
    private readonly ResetSystemAdminPasswordCommandHandler _resetPasswordHandler;

    public SystemAdminsController(
        ListSystemAdminsQueryHandler listHandler,
        CreateSystemAdminCommandHandler createHandler,
        ResetSystemAdminPasswordCommandHandler resetPasswordHandler)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
        _resetPasswordHandler = resetPasswordHandler;
    }

    /// <summary>One field-specific parameter per "Search By" choice - see SearchCriteria. `search`
    /// is the legacy generic parameter, kept working and treated as a name search.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? name = null, [FromQuery] string? email = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null, CancellationToken cancellationToken = default)
    {
        var criteria = SearchCriteria.From(name: name ?? search, email: email);
        var result = await _listHandler.Handle(page, pageSize, criteria, active, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSystemAdminCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Administrative reset by a SuperAdmin - no current password required.</summary>
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(
        int id, ResetSystemAdminPasswordCommand command, CancellationToken cancellationToken)
    {
        await _resetPasswordHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
