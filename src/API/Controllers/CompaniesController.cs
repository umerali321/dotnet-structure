using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Commands.CreateCompany;
using SkillsetsBackend.Application.Companies.Queries.ListCompanies;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ListCompaniesQueryHandler _listHandler;
    private readonly CreateCompanyCommandHandler _createHandler;

    public CompaniesController(ListCompaniesQueryHandler listHandler, CreateCompanyCommandHandler createHandler)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _listHandler.Handle(new ListCompaniesQuery(search), GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var companyId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { companyId });
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
