using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SkillTrax.Commands.CreateSkillTrax;
using SkillsetsBackend.Application.SkillTrax.Commands.UpdateSkillTrax;
using SkillsetsBackend.Application.SkillTrax.Commands.DeleteSkillTrax;
using SkillsetsBackend.Application.SkillTrax.Queries.GetSkillTraxDetail;
using SkillsetsBackend.Application.SkillTrax.Queries.ListSkillTrax;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/skilltrax")]
[Authorize]
public class SkillTraxController : ControllerBase
{
    private readonly CreateSkillTraxCommandHandler _createHandler;
    private readonly UpdateSkillTraxCommandHandler _updateHandler;
    private readonly DeleteSkillTraxCommandHandler _deleteHandler;
    private readonly ListSkillTraxQueryHandler _listHandler;
    private readonly GetSkillTraxDetailQueryHandler _getDetailHandler;

    public SkillTraxController(
        CreateSkillTraxCommandHandler createHandler,
        UpdateSkillTraxCommandHandler updateHandler,
        DeleteSkillTraxCommandHandler deleteHandler,
        ListSkillTraxQueryHandler listHandler,
        GetSkillTraxDetailQueryHandler getDetailHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _listHandler = listHandler;
        _getDetailHandler = getDetailHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _listHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
    {
        var result = await _getDetailHandler.Handle(id, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSkillTraxCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateSkillTraxCommand command, CancellationToken cancellationToken)
    {
        var result = await _updateHandler.Handle(id, command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _deleteHandler.Handle(id, GetCaller(), cancellationToken);
        return NoContent();
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
