using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Auth.Commands.Login;
using SkillsetsBackend.Application.Auth.Commands.Logout;
using SkillsetsBackend.Application.Auth.Commands.Refresh;
using SkillsetsBackend.Application.Auth.Commands.SwitchCompany;
using SkillsetsBackend.Application.Auth.DTOs;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly SwitchCompanyCommandHandler _switchCompanyHandler;

    public AuthController(
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshHandler,
        LogoutCommandHandler logoutHandler,
        SwitchCompanyCommandHandler switchCompanyHandler)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _switchCompanyHandler = switchCompanyHandler;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.Handle(command, GetClientIp(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _refreshHandler.Handle(command, GetClientIp(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await _logoutHandler.Handle(command, GetClientIp(), cancellationToken);
        return NoContent();
    }

    /// <summary>Switches the active company for the current session and issues new tokens scoped to it.</summary>
    [HttpPost("switch-company")]
    [Authorize]
    public async Task<ActionResult<AuthResultDto>> SwitchCompany(SwitchCompanyCommand command, CancellationToken cancellationToken)
    {
        var result = await _switchCompanyHandler.Handle(
            command,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.FindFirstValue(ClaimTypes.Email)!,
            User.FindFirstValue(ClaimTypes.Role)!,
            GetClientIp(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var companyIdClaim = User.FindFirstValue(AuthClaimTypes.CompanyId);

        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            role = User.FindFirstValue(ClaimTypes.Role),
            companyId = companyIdClaim is null ? (int?)null : int.Parse(companyIdClaim),
            companyName = User.FindFirstValue(AuthClaimTypes.CompanyName),
        });
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
