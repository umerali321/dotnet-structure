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
    private readonly ICurrentCompanyContext _currentCompanyContext;

    public SkillsoftController(ISkillsoftSsoService ssoService, ICurrentCompanyContext currentCompanyContext)
    {
        _ssoService = ssoService;
        _currentCompanyContext = currentCompanyContext;
    }

    [HttpGet("launch-ticket")]
    [Authorize]
    public async Task<IActionResult> CreateLaunchTicket(CancellationToken cancellationToken)
    {
        var companyId = _currentCompanyContext.CompanyId
            ?? throw new UnauthorizedAccessException("Select a company before accessing the course library.");

        var ticket = await _ssoService.CreateLaunchTicketAsync(GetCaller(), companyId, cancellationToken);
        var launchUrl = Url.Action(nameof(Launch), "Skillsoft", new { version = "1.0", ticket }, Request.Scheme)!;

        return Ok(new { launchUrl });
    }

    [HttpGet("launch")]
    [AllowAnonymous]
    public async Task<IActionResult> Launch([FromQuery] string ticket, CancellationToken cancellationToken)
    {
        var result = await _ssoService.ConsumeLaunchTicketAsync(ticket, cancellationToken);
        return Content(result.AutoPostHtml, "text/html");
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
