using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.Interfaces;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/skillsoft")]
public class SkillsoftController : ControllerBase
{
    private readonly ISkillsoftSsoService _ssoService;
    private readonly ISkillsoftCatalogService _catalogService;
    private readonly ISkillsoftTranscriptService _transcriptService;
    private readonly ICurrentCompanyContext _currentCompanyContext;

    public SkillsoftController(
        ISkillsoftSsoService ssoService,
        ISkillsoftCatalogService catalogService,
        ISkillsoftTranscriptService transcriptService,
        ICurrentCompanyContext currentCompanyContext)
    {
        _ssoService = ssoService;
        _catalogService = catalogService;
        _transcriptService = transcriptService;
        _currentCompanyContext = currentCompanyContext;
    }

    [HttpGet("launch-ticket")]
    [Authorize]
    public async Task<IActionResult> CreateLaunchTicket(CancellationToken cancellationToken)
    {
        var companyId = RequireCompanyId();

        var ticket = await _ssoService.CreateLaunchTicketAsync(GetCaller(), companyId, cancellationToken);
        var launchUrl = Url.Action(nameof(Launch), "Skillsoft", new { version = "1.0", ticket }, Request.Scheme)!;

        return Ok(new { launchUrl });
    }

    [HttpGet("launch")]
    [AllowAnonymous]
    public async Task<IActionResult> Launch([FromQuery] string ticket, CancellationToken cancellationToken)
    {
        var result = await _ssoService.ConsumeLaunchTicketAsync(ticket, cancellationToken);

        return Redirect(result.RedirectUrl);
    }

    [HttpGet("session-status")]
    [Authorize]
    public async Task<IActionResult> GetSessionStatus(CancellationToken cancellationToken)
    {
        var companyId = RequireCompanyId();
        var status = await _ssoService.GetSessionStatusAsync(GetCaller(), companyId, cancellationToken);
        return Ok(status);
    }

    [HttpPost("start-session")]
    [Authorize]
    public async Task<IActionResult> StartSession(CancellationToken cancellationToken)
    {
        var companyId = RequireCompanyId();
        var ticket = await _ssoService.StartSessionAsync(GetCaller(), companyId, cancellationToken);
        var launchUrl = Url.Action(nameof(Launch), "Skillsoft", new { version = "1.0", ticket }, Request.Scheme)!;

        return Ok(new { launchUrl });
    }

    [HttpGet("catalog/search")]
    [Authorize]
    public async Task<IActionResult> SearchCatalog(
        [FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var companyId = RequireCompanyId();
        var result = await _catalogService.SearchAsync(GetCaller(), companyId, q, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("catalog/{assetId}")]
    [Authorize]
    public async Task<IActionResult> GetAssetDetail(string assetId, CancellationToken cancellationToken)
    {
        var companyId = RequireCompanyId();
        var result = await _catalogService.GetAssetDetailAsync(GetCaller(), companyId, assetId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("courses/{assetId}/launch")]
    [Authorize]
    public async Task<IActionResult> LaunchCourse(string assetId, CancellationToken cancellationToken)
    {
        var companyId = RequireCompanyId();
        var launchUrl = await _ssoService.GetCourseLaunchUrlAsync(GetCaller(), companyId, assetId, cancellationToken);
        return Ok(new { launchUrl });
    }

    [HttpGet("transcript")]
    [Authorize]
    public async Task<IActionResult> GetTranscript(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var companyId = RequireCompanyId();
        var result = await _transcriptService.GetTranscriptAsync(GetCaller(), companyId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    private int RequireCompanyId() =>
        _currentCompanyContext.CompanyId
            ?? throw new UnauthorizedAccessException("Select a company before accessing the course library.");

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
