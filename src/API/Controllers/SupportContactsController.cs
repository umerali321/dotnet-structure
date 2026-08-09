using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SupportContacts.Commands.CreateSupportContact;
using SkillsetsBackend.Application.SupportContacts.Commands.DeactivateSupportContact;
using SkillsetsBackend.Application.SupportContacts.Commands.UpdateSupportContact;
using SkillsetsBackend.Application.SupportContacts.Queries.GetSupportContactById;
using SkillsetsBackend.Application.SupportContacts.Queries.ListSupportContacts;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support-contacts")]
[Authorize]
public class SupportContactsController : ControllerBase
{
    private readonly ListSupportContactsQueryHandler _listHandler;
    private readonly GetSupportContactByIdQueryHandler _getByIdHandler;
    private readonly CreateSupportContactCommandHandler _createHandler;
    private readonly UpdateSupportContactCommandHandler _updateHandler;
    private readonly DeactivateSupportContactCommandHandler _deactivateHandler;

    public SupportContactsController(
        ListSupportContactsQueryHandler listHandler,
        GetSupportContactByIdQueryHandler getByIdHandler,
        CreateSupportContactCommandHandler createHandler,
        UpdateSupportContactCommandHandler updateHandler,
        DeactivateSupportContactCommandHandler deactivateHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SupportContactListRequest request, CancellationToken cancellationToken)
    {
        var query = new ListSupportContactsQuery(request.Page, request.PageSize, request.CompanyId, request.IsActive);
        var result = await _listHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(id, GetCaller(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupportContactCommand command, CancellationToken cancellationToken)
    {
        var newId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(newId, GetCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = newId, version = "1.0" }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSupportContactCommand command, CancellationToken cancellationToken)
    {
        await _updateHandler.Handle(id, command, GetCaller(), cancellationToken);
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

public class SupportContactListRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public int? CompanyId { get; set; }

    public bool? IsActive { get; set; }
}
