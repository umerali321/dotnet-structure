using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.Commands.SaveSmtpSettings;
using SkillsetsBackend.Application.Settings.Commands.SendTestEmail;
using SkillsetsBackend.Application.Settings.Commands.TestSmtpConnection;
using SkillsetsBackend.Application.Settings.Queries.GetSmtpSettings;
using SkillsetsBackend.Application.Settings.Queries.ListEmailHistory;
using SkillsetsBackend.Application.Settings.Queries.GetEmailLogDetail;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly GetSmtpSettingsQueryHandler _getSmtpSettingsHandler;
    private readonly SaveSmtpSettingsCommandHandler _saveSmtpSettingsHandler;
    private readonly TestSmtpConnectionCommandHandler _testSmtpConnectionHandler;
    private readonly SendTestEmailCommandHandler _sendTestEmailHandler;
    private readonly ListEmailHistoryQueryHandler _listEmailHistoryHandler;
    private readonly GetEmailLogDetailQueryHandler _getEmailLogDetailHandler;

    public SettingsController(
        GetSmtpSettingsQueryHandler getSmtpSettingsHandler,
        SaveSmtpSettingsCommandHandler saveSmtpSettingsHandler,
        TestSmtpConnectionCommandHandler testSmtpConnectionHandler,
        SendTestEmailCommandHandler sendTestEmailHandler,
        ListEmailHistoryQueryHandler listEmailHistoryHandler,
        GetEmailLogDetailQueryHandler getEmailLogDetailHandler)
    {
        _getSmtpSettingsHandler = getSmtpSettingsHandler;
        _saveSmtpSettingsHandler = saveSmtpSettingsHandler;
        _testSmtpConnectionHandler = testSmtpConnectionHandler;
        _sendTestEmailHandler = sendTestEmailHandler;
        _listEmailHistoryHandler = listEmailHistoryHandler;
        _getEmailLogDetailHandler = getEmailLogDetailHandler;
    }

    [HttpGet("smtp")]
    public async Task<IActionResult> GetSmtpSettings(CancellationToken cancellationToken)
    {
        var result = await _getSmtpSettingsHandler.Handle(GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("smtp")]
    public async Task<IActionResult> SaveSmtpSettings(SaveSmtpSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await _saveSmtpSettingsHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("smtp/test-connection")]
    public async Task<IActionResult> TestSmtpConnection(TestSmtpConnectionCommand command, CancellationToken cancellationToken)
    {
        var result = await _testSmtpConnectionHandler.Handle(command, GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("smtp/test-email")]
    public async Task<IActionResult> SendTestEmail(SendTestEmailCommand command, CancellationToken cancellationToken)
    {
        await _sendTestEmailHandler.Handle(command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpGet("email-history")]
    public async Task<IActionResult> ListEmailHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _listEmailHistoryHandler.Handle(new ListEmailHistoryQuery(page, pageSize, search), GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("email-history/{id:int}")]
    public async Task<IActionResult> GetEmailLogDetail(int id, CancellationToken cancellationToken)
    {
        var result = await _getEmailLogDetailHandler.Handle(id, GetCaller(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
