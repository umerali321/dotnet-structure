using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibrarySessionHistory;
using SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibraryUsers;
using SkillsetsBackend.Application.Dashboard.Queries.GetDashboardStats;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly GetDashboardStatsQueryHandler _statsHandler;
    private readonly GetCourseLibraryUsersQueryHandler _courseLibraryUsersHandler;
    private readonly GetCourseLibrarySessionHistoryQueryHandler _sessionHistoryHandler;

    public DashboardController(
        GetDashboardStatsQueryHandler statsHandler,
        GetCourseLibraryUsersQueryHandler courseLibraryUsersHandler,
        GetCourseLibrarySessionHistoryQueryHandler sessionHistoryHandler)
    {
        _statsHandler = statsHandler;
        _courseLibraryUsersHandler = courseLibraryUsersHandler;
        _sessionHistoryHandler = sessionHistoryHandler;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] DashboardStatsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetDashboardStatsQuery(request.CompanyId, request.StartDate, request.EndDate);
        var result = await _statsHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("course-library-users")]
    public async Task<IActionResult> GetCourseLibraryUsers([FromQuery] CourseLibraryUsersRequest request, CancellationToken cancellationToken)
    {
        var query = new GetCourseLibraryUsersQuery(
            request.CompanyId, request.StartDate, request.EndDate, request.Search, request.Page, request.PageSize);
        var result = await _courseLibraryUsersHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("course-library-users/history")]
    public async Task<IActionResult> GetCourseLibrarySessionHistory([FromQuery] CourseLibrarySessionHistoryRequest request, CancellationToken cancellationToken)
    {
        var query = new GetCourseLibrarySessionHistoryQuery(request.Email, request.CompanyId);
        var result = await _sessionHistoryHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public class DashboardStatsRequest
{
    public int? CompanyId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}

public class CourseLibraryUsersRequest
{
    public int? CompanyId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}

public class CourseLibrarySessionHistoryRequest
{
    public string Email { get; set; } = string.Empty;

    public int? CompanyId { get; set; }
}
