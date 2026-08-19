using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Commands.ActivateCompany;
using SkillsetsBackend.Application.Companies.Commands.CreateCompany;
using SkillsetsBackend.Application.Companies.Commands.DeactivateCompany;
using SkillsetsBackend.Application.Companies.Commands.SetCompanyLicense;
using SkillsetsBackend.Application.Companies.Commands.UpdateCompany;
using SkillsetsBackend.Application.Companies.Commands.UpdateCompanyLogo;
using SkillsetsBackend.Application.Companies.Queries.GetCompanyById;
using SkillsetsBackend.Application.Companies.Queries.ListCompanies;

namespace SkillsetsBackend.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/svg+xml",
    };
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024;

    private readonly ListCompaniesQueryHandler _listHandler;
    private readonly GetCompanyByIdQueryHandler _getByIdHandler;
    private readonly CreateCompanyCommandHandler _createHandler;
    private readonly UpdateCompanyCommandHandler _updateHandler;
    private readonly DeactivateCompanyCommandHandler _deactivateHandler;
    private readonly ActivateCompanyCommandHandler _activateHandler;
    private readonly SetCompanyLicenseCommandHandler _setLicenseHandler;
    private readonly UpdateCompanyLogoCommandHandler _updateLogoHandler;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CompaniesController(
        ListCompaniesQueryHandler listHandler,
        GetCompanyByIdQueryHandler getByIdHandler,
        CreateCompanyCommandHandler createHandler,
        UpdateCompanyCommandHandler updateHandler,
        DeactivateCompanyCommandHandler deactivateHandler,
        ActivateCompanyCommandHandler activateHandler,
        SetCompanyLicenseCommandHandler setLicenseHandler,
        UpdateCompanyLogoCommandHandler updateLogoHandler,
        IWebHostEnvironment webHostEnvironment)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
        _activateHandler = activateHandler;
        _setLicenseHandler = setLicenseHandler;
        _updateLogoHandler = updateLogoHandler;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] bool includeInactive, [FromQuery] int page = 1, [FromQuery] int pageSize = 100,
        [FromQuery] string? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.Handle(new ListCompaniesQuery(search, includeInactive, page, pageSize, statusFilter), GetCaller(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(id, GetCaller(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var companyId = await _createHandler.Handle(command, GetCaller(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { companyId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        await _updateHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _deactivateHandler.Handle(id, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        await _activateHandler.Handle(id, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/license")]
    public async Task<IActionResult> SetLicense(int id, SetCompanyLicenseCommand command, CancellationToken cancellationToken)
    {
        await _setLicenseHandler.Handle(id, command, GetCaller(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/logo")]
    [RequestSizeLimit(MaxLogoSizeBytes)]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > MaxLogoSizeBytes)
        {
            return BadRequest(new { message = "Logo must be 2MB or smaller." });
        }

        if (!AllowedLogoContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Logo must be a PNG, JPEG, WEBP, or SVG image." });
        }

        var extension = file.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase)
            ? ".svg"
            : "." + file.ContentType.Split('/')[1];

        var webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var logoDirectory = Path.Combine(webRootPath, "company-logos");
        Directory.CreateDirectory(logoDirectory);

        var fileName = $"{id}{extension}";
        var filePath = Path.Combine(logoDirectory, fileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var logoUrl = $"/company-logos/{fileName}";
        await _updateLogoHandler.Handle(id, logoUrl, GetCaller(), cancellationToken);
        return Ok(new { logoUrl });
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
