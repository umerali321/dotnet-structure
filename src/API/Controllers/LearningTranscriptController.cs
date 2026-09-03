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

    /// <summary>Mirrors the on-screen columns exactly (Employee, Type, Company, Course, Status,
    /// Completion Date, High Score, Expected/Actual Duration) - the export is meant to be exactly
    /// what the screen currently shows, not the full underlying record.</summary>
    private static byte[] BuildExportFile(IReadOnlyCollection<LearningTranscriptListItemDto> items)
    {
        string[] headers =
        [
            "Employee", "Type", "Company", "Course", "Status", "Completion Date", "High Score",
            "Expected Duration", "Actual Duration",
        ];

        var rows = items.Select(i => (IReadOnlyList<string>)
        [
            $"{i.EmployeeFirstName} {i.EmployeeLastName}".Trim(),
            i.StudentType ?? "",
            i.CompanyName ?? "",
            i.AssetTitle,
            i.CompletionStatus ?? "",
            i.CompletionDate?.ToString("yyyy-MM-dd") ?? "",
            i.HighScore?.ToString("0.##") ?? "",
            FormatDurationHms(i.ExpectedDurationMinutes),
            FormatDurationHms(i.ActualDurationMinutes),
        ]);

        return ExcelExportWriter.Write("Learning Transcript", headers, rows);
    }

    /// <summary>Matches the frontend's formatDurationHms exactly, so the export reads the same as
    /// the screen (e.g. 540 minutes -> "9h 0m 0s") rather than a raw minute count.</summary>
    private static string FormatDurationHms(int? totalMinutes)
    {
        if (totalMinutes is null) return "";

        var totalSeconds = totalMinutes.Value * 60;
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        return $"{hours}h {minutes}m {seconds}s";
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
