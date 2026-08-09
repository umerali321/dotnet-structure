using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Faqs.Commands.CreateFaq;
using SkillsetsBackend.Application.Faqs.Commands.DeactivateFaq;
using SkillsetsBackend.Application.Faqs.Commands.UpdateFaq;
using SkillsetsBackend.Application.Faqs.Queries.GetFaqById;
using SkillsetsBackend.Application.Faqs.Queries.ListFaqs;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/faqs")]
[Authorize]
public class FaqsController : ControllerBase
{
    private readonly ListFaqsQueryHandler _listHandler;
    private readonly GetFaqByIdQueryHandler _getByIdHandler;
    private readonly CreateFaqCommandHandler _createHandler;
    private readonly UpdateFaqCommandHandler _updateHandler;
    private readonly DeactivateFaqCommandHandler _deactivateHandler;

    public FaqsController(
        ListFaqsQueryHandler listHandler,
        GetFaqByIdQueryHandler getByIdHandler,
        CreateFaqCommandHandler createHandler,
        UpdateFaqCommandHandler updateHandler,
        DeactivateFaqCommandHandler deactivateHandler)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FaqListRequest request, CancellationToken cancellationToken)
    {
        var query = new ListFaqsQuery(request.Page, request.PageSize, request.Search, request.CompanyId, request.IsActive);
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
    public async Task<IActionResult> Create(CreateFaqCommand command, CancellationToken cancellationToken)
    {
        var newFaqId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        var created = await _getByIdHandler.Handle(newFaqId, GetCaller(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = newFaqId, version = "1.0" }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateFaqCommand command, CancellationToken cancellationToken)
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

public class FaqListRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? Search { get; set; }

    public int? CompanyId { get; set; }

    public bool? IsActive { get; set; }
}
