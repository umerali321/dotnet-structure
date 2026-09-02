using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.Commands.ImportRoster;
using SkillsetsBackend.Application.RosterImport.Commands.PreviewRosterImport;
using SkillsetsBackend.Application.RosterImport.Commands.SendRosterWelcomeEmails;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Queries.GetCreationSourceStats;
using SkillsetsBackend.Application.RosterImport.Queries.GetRosterImportBatch;
using SkillsetsBackend.Infrastructure.Common;

namespace SkillsetsBackend.API.Controllers;

/// <summary>
/// Bulk Employee Roster Import. Mirrors the Company Import endpoints, with two differences the
/// workflow requires: a preview step that writes nothing, and a separate confirm step for the
/// welcome emails so nobody is emailed before the admin has seen the results.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roster-import")]
[Authorize]
public class RosterImportController : ControllerBase
{
    private const long MaxImportFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xls", ".csv" };

    private readonly PreviewRosterImportCommandHandler _previewHandler;
    private readonly ImportRosterCommandHandler _importHandler;
    private readonly SendRosterWelcomeEmailsCommandHandler _welcomeEmailsHandler;
    private readonly GetRosterImportBatchQueryHandler _getBatchHandler;
    private readonly GetCreationSourceStatsQueryHandler _statsHandler;

    public RosterImportController(
        PreviewRosterImportCommandHandler previewHandler,
        ImportRosterCommandHandler importHandler,
        SendRosterWelcomeEmailsCommandHandler welcomeEmailsHandler,
        GetRosterImportBatchQueryHandler getBatchHandler,
        GetCreationSourceStatsQueryHandler statsHandler)
    {
        _previewHandler = previewHandler;
        _importHandler = importHandler;
        _welcomeEmailsHandler = welcomeEmailsHandler;
        _getBatchHandler = getBatchHandler;
        _statsHandler = statsHandler;
    }

    /// <summary>Dry run - reports exactly what an import would do and writes nothing.</summary>
    [HttpPost("preview")]
    [RequestSizeLimit(MaxImportFileSizeBytes)]
    public async Task<IActionResult> Preview(IFormFile file, [FromQuery] int? companyId, CancellationToken cancellationToken)
    {
        if (Validate(file) is { } error)
        {
            return error;
        }

        await using var stream = file.OpenReadStream();
        var result = await _previewHandler.Handle(
            new PreviewRosterImportCommand(stream, file.FileName, companyId), GetCaller(), cancellationToken);

        return Ok(result);
    }

    /// <summary>Creates the accounts. Sends no email - see the welcome-emails endpoint.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxImportFileSizeBytes)]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] int? companyId, CancellationToken cancellationToken)
    {
        if (Validate(file) is { } error)
        {
            return error;
        }

        await using var stream = file.OpenReadStream();
        var result = await _importHandler.Handle(
            new ImportRosterCommand(stream, file.FileName, companyId), GetCaller(), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{batchId:int}")]
    public async Task<IActionResult> GetBatch(int batchId, CancellationToken cancellationToken) =>
        Ok(await _getBatchHandler.Handle(batchId, GetCaller(), cancellationToken));

    /// <summary>The admin's answer to "Send Welcome Emails?" - "no" is recorded too, so the prompt
    /// closes either way.</summary>
    [HttpPost("{batchId:int}/welcome-emails")]
    public async Task<IActionResult> SendWelcomeEmails(
        int batchId, [FromBody] SendWelcomeEmailsRequest request, CancellationToken cancellationToken) =>
        Ok(await _welcomeEmailsHandler.Handle(
            new SendRosterWelcomeEmailsCommand(batchId, request.Send), GetCaller(), cancellationToken));

    /// <summary>Downloads the results table as .xlsx, through the same ExcelExportWriter the
    /// Students/Managers/Companies exports use.</summary>
    [HttpGet("{batchId:int}/export")]
    public async Task<IActionResult> ExportResults(int batchId, CancellationToken cancellationToken)
    {
        var batch = await _getBatchHandler.Handle(batchId, GetCaller(), cancellationToken);

        var bytes = ExcelExportWriter.Write(
            "Import Results",
            ["Row", "Name", "Email", "Company", "Employee Type", "Mgr Dashboard", "Status", "Reason"],
            batch.Rows.Select(r => new[]
            {
                r.RowNumber.ToString(),
                r.Name ?? string.Empty,
                r.Email ?? string.Empty,
                r.CompanyName ?? string.Empty,
                r.EmployeeType ?? string.Empty,
                r.GiveManagerDashboard ? "Yes" : "No",
                r.Status,
                r.Reason,
            }));

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"roster-import-{batchId}-results.xlsx");
    }

    /// <summary>Manual vs roster-import counts for employees and managers.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? companyId, CancellationToken cancellationToken) =>
        Ok(await _statsHandler.Handle(companyId, GetCaller(), cancellationToken));

    private BadRequestObjectResult? Validate(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > MaxImportFileSizeBytes)
        {
            return BadRequest(new { message = "Import file must be 10MB or smaller." });
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
        {
            return BadRequest(new { message = "Only .xlsx, .xls and .csv files are supported." });
        }

        return null;
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}

public record SendWelcomeEmailsRequest(bool Send);
