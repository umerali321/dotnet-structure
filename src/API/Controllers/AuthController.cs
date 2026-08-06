using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Auth.Commands.Login;
using SkillsetsBackend.Application.Auth.Commands.Logout;
using SkillsetsBackend.Application.Auth.Commands.Refresh;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;
    private readonly LogoutCommandHandler _logoutHandler;

    public AuthController(
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshHandler,
        LogoutCommandHandler logoutHandler)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
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

    [HttpGet("me")]
    [Authorize(Policy = Roles.SuperAdmin)]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            role = User.FindFirstValue(ClaimTypes.Role),
        });
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
