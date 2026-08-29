using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.LearningTranscript.Commands.ImportLearningTranscriptBatch;
using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Application.LearningTranscript.Queries.GetLearningTranscriptStats;
using SkillsetsBackend.Application.LearningTranscript.Queries.ListLearningTranscript;
using SkillsetsBackend.Infrastructure.Common;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/learning-transcript")]
[Authorize]
public class LearningTranscriptController : ControllerBase
{
    private readonly ListLearningTranscriptQueryHandler _listHandler;
    private readonly GetLearningTranscriptStatsQueryHandler _statsHandler;
    private readonly ImportLearningTranscriptBatchCommandHandler _importHandler;

    public LearningTranscriptController(
        ListLearningTranscriptQueryHandler listHandler,
        GetLearningTranscriptStatsQueryHandler statsHandler,
        ImportLearningTranscriptBatchCommandHandler importHandler)
    {
        _listHandler = listHandler;
        _statsHandler = statsHandler;
        _importHandler = importHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListLearningTranscriptRequest request, CancellationToken cancellationToken)
    {
        var query = new ListLearningTranscriptQuery(
            request.Page, request.PageSize, request.Search, request.CompanyId,
            request.AssetId, request.CompletionStatus, request.DateFrom, request.DateTo);

        var result = await _listHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] LearningTranscriptStatsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetLearningTranscriptStatsQuery(request.CompanyId, request.DateFrom, request.DateTo);
        var result = await _statsHandler.Handle(query, GetCaller(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Same filters as List, but every matching row in one file instead of one page - the
    /// "Export" button downloads exactly what the current search/filter combination would show.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ListLearningTranscriptRequest request, CancellationToken cancellationToken)
    {
        var query = new ListLearningTranscriptQuery(
            1, ListLearningTranscriptQueryHandler.MaxPageSize, request.Search, request.CompanyId,
            request.AssetId, request.CompletionStatus, request.DateFrom, request.DateTo);

        var result = await _listHandler.Handle(query, GetCaller(), cancellationToken);
        var bytes = BuildExportFile(result.Items);
        return File(bytes, ExcelExportWriter.ContentType, "LearningTranscript.xlsx");
    }

    [HttpPost("import")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var stream = file.OpenReadStream();
        var command = new ImportLearningTranscriptBatchCommand(stream, file.FileName);
        var result = await _importHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    private static byte[] BuildExportFile(IReadOnlyCollection<LearningTranscriptListItemDto> items)
    {
        string[] headers =
        [
            "Employee", "Email", "Company", "Manager", "User Status", "Group Name", "Group Org Code", "Group Path",
            "Total Sessions", "Course", "Course Type", "Asset ID", "Enrollment Date", "First Access", "Last Access",
            "Completion Date", "Status", "Pre-test Score", "High Score", "Current Score", "Attempts",
            "Expected Duration (min)", "Actual Duration (min)", "Times Accessed", "Last Skillport Login",
            "Skillport Registration Date",
        ];

        var rows = items.Select(i => (IReadOnlyList<string>)
        [
            $"{i.EmployeeFirstName} {i.EmployeeLastName}".Trim(),
            i.EmployeeEmail ?? "",
            i.CompanyName ?? "",
            $"{i.ManagerFirstName} {i.ManagerLastName}".Trim(),
            i.UserStatus ?? "",
            i.GroupName ?? "",
            i.GroupOrgCode ?? "",
            i.GroupPath ?? "",
            i.TotalSessions.ToString(),
            i.AssetTitle,
            i.AssetType ?? "",
            i.AssetId,
            i.EnrollmentDate?.ToString("yyyy-MM-dd") ?? "",
            i.FirstAccessDate?.ToString("yyyy-MM-dd") ?? "",
            i.LastAccessDate?.ToString("yyyy-MM-dd") ?? "",
            i.CompletionDate?.ToString("yyyy-MM-dd") ?? "",
            i.CompletionStatus ?? "",
            i.PreTestScore?.ToString("0.##") ?? "",
            i.HighScore?.ToString("0.##") ?? "",
            i.CurrentScore?.ToString("0.##") ?? "",
            i.ActualTestAttempts?.ToString() ?? "",
            i.ExpectedDurationMinutes?.ToString() ?? "",
            i.ActualDurationMinutes?.ToString() ?? "",
            i.TimesAccessed?.ToString() ?? "",
            i.LastSkillportLoginDate?.ToString("yyyy-MM-dd") ?? "",
            i.SkillportRegistrationDate?.ToString("yyyy-MM-dd") ?? "",
        ]);

        return ExcelExportWriter.Write("Learning Transcript", headers, rows);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public class ListLearningTranscriptRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? Search { get; set; }

    public int? CompanyId { get; set; }

    public string? AssetId { get; set; }

    public string? CompletionStatus { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }
}

public class LearningTranscriptStatsRequest
{
    public int? CompanyId { get; set; }

    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }
}
