using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Scraper.Commands.StartScraperRun;
using SkillsetsBackend.Application.Scraper.Commands.StopScraperRun;
using SkillsetsBackend.Application.Scraper.Queries.GetScraperRunStatus;
using SkillsetsBackend.Application.Scraper.Queries.GetScraperSqlFile;
using SkillsetsBackend.Application.Scraper.Queries.ListScraperCategories;

namespace SkillsetsBackend.API.Controllers;

/// <summary>SuperAdmin-only process-orchestration for the Skillport course scraper - a distinct
/// bounded context from SkillsoftController (which serves the already-scraped course library
/// data), so it gets its own controller rather than growing an already multi-purpose one.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scraper")]
[Authorize]
public class ScraperController : ControllerBase
{
    private readonly StartScraperRunCommandHandler _startHandler;
    private readonly StopScraperRunCommandHandler _stopHandler;
    private readonly GetScraperRunStatusQueryHandler _statusHandler;
    private readonly ListScraperCategoriesQueryHandler _categoriesHandler;
    private readonly GetScraperSqlFileQueryHandler _sqlFileHandler;

    public ScraperController(
        StartScraperRunCommandHandler startHandler,
        StopScraperRunCommandHandler stopHandler,
        GetScraperRunStatusQueryHandler statusHandler,
        ListScraperCategoriesQueryHandler categoriesHandler,
        GetScraperSqlFileQueryHandler sqlFileHandler)
    {
        _startHandler = startHandler;
        _stopHandler = stopHandler;
        _statusHandler = statusHandler;
        _categoriesHandler = categoriesHandler;
        _sqlFileHandler = sqlFileHandler;
    }

    [HttpPost("runs")]
    public async Task<IActionResult> StartRun(StartScraperRunCommand command, CancellationToken cancellationToken)
    {
        var result = await _startHandler.Handle(command, GetCaller(), cancellationToken);
        return StatusCode(StatusCodes.Status202Accepted, result);
    }

    // Kills the whole process tree (python + chromedriver + chrome) in one shot - more reliable
    // than manually ending python.exe in Task Manager, which can leave orphaned chromedriver/
    // chrome processes behind.
    [HttpPost("runs/current/stop")]
    public async Task<IActionResult> StopRun(CancellationToken cancellationToken)
    {
        var result = await _stopHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("runs/current")]
    public async Task<IActionResult> GetCurrentRun(CancellationToken cancellationToken)
    {
        var result = await _statusHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
    {
        var result = await _categoriesHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    // Not served via wwwroot/UseStaticFiles (unlike company logos, which are intentionally public) -
    // this is a ready-to-run DB mutation script plus a bulk data dump, so it stays behind
    // [Authorize] + the SuperAdmin gate like every other endpoint here.
    [HttpGet("runs/current/sql-file")]
    public async Task<IActionResult> DownloadCurrentSqlFile(CancellationToken cancellationToken)
    {
        var result = await _sqlFileHandler.Handle(GetCaller(), cancellationToken);
        return File(result.Content, "application/sql", result.FileName);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
