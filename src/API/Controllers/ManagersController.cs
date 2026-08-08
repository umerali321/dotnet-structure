using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;
using SkillsetsBackend.Application.Managers.Commands.CreateManager;
using SkillsetsBackend.Application.Managers.Commands.UpdateManager;
using SkillsetsBackend.Application.Managers.Queries.GetManagerById;
using SkillsetsBackend.Application.Managers.Queries.GetManagerCompanies;
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
    private readonly CreateManagerCommandHandler _createHandler;
    private readonly UpdateManagerCommandHandler _updateHandler;
    private readonly ChangeManagerPasswordCommandHandler _changePasswordHandler;

    public ManagersController(
        ListManagersQueryHandler listHandler,
        GetManagerByIdQueryHandler getByIdHandler,
        GetManagerCompaniesQueryHandler getCompaniesHandler,
        GetManagerRolesQueryHandler getRolesHandler,
        CreateManagerCommandHandler createHandler,
        UpdateManagerCommandHandler updateHandler,
        ChangeManagerPasswordCommandHandler changePasswordHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _getCompaniesHandler = getCompaniesHandler;
        _getRolesHandler = getRolesHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _changePasswordHandler = changePasswordHandler;
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

    [HttpPost]
    public async Task<IActionResult> Create(CreateManagerCommand command, CancellationToken cancellationToken)
    {
        var newUserId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(newUserId, GetCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = newUserId, version = "1.0" }, created);
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

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
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
}
