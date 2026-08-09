using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Support.Commands.CreateSupportRequest;
using SkillsetsBackend.Application.Support.Commands.UpdateSupportRequestStatus;
using SkillsetsBackend.Application.Support.Queries.GetSupportRequestById;
using SkillsetsBackend.Application.Support.Queries.ListSupportRequests;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support-requests")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly ListSupportRequestsQueryHandler _listHandler;
    private readonly GetSupportRequestByIdQueryHandler _getByIdHandler;
    private readonly CreateSupportRequestCommandHandler _createHandler;
    private readonly UpdateSupportRequestStatusCommandHandler _updateStatusHandler;

    public SupportController(
        ListSupportRequestsQueryHandler listHandler,
        GetSupportRequestByIdQueryHandler getByIdHandler,
        CreateSupportRequestCommandHandler createHandler,
        UpdateSupportRequestStatusCommandHandler updateStatusHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateStatusHandler = updateStatusHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SupportRequestListRequest request, CancellationToken cancellationToken)
    {
        var query = new ListSupportRequestsQuery(request.Page, request.PageSize, request.CompanyId, request.Status);
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
    public async Task<IActionResult> Create(CreateSupportRequestCommand command, CancellationToken cancellationToken)
    {
        var newId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(newId, GetCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = newId, version = "1.0" }, created);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateSupportRequestStatusCommand command, CancellationToken cancellationToken)
    {
        await _updateStatusHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public class SupportRequestListRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public int? CompanyId { get; set; }

    public string? Status { get; set; }
}
