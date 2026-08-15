using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Auth.Queries.ListLoginActivityLogs;
using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system-logs")]
[Authorize]
public class SystemLogsController : ControllerBase
{
    private readonly ListLoginActivityLogsQueryHandler _listHandler;

    public SystemLogsController(ListLoginActivityLogsQueryHandler listHandler)
    {
        _listHandler = listHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? eventType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.Handle(new ListLoginActivityLogsQuery(page, pageSize, eventType), GetCaller(), cancellationToken);
        return Ok(result);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
