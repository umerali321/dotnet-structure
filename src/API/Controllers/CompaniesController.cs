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
using SkillsetsBackend.Application.Companies.Commands.ImportCompanies;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Application.Companies.Queries.GetCompanyById;
using SkillsetsBackend.Application.Companies.Queries.GetCompanyLogo;
using SkillsetsBackend.Application.Companies.Queries.ListCompanies;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Infrastructure.Common;

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

    private static readonly HashSet<string> AllowedImportExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".csv" };
    private const long MaxImportFileSizeBytes = 10 * 1024 * 1024;

    private readonly ListCompaniesQueryHandler _listHandler;
    private readonly GetCompanyByIdQueryHandler _getByIdHandler;
    private readonly GetCompanyLogoQueryHandler _getLogoHandler;
    private readonly CreateCompanyCommandHandler _createHandler;
    private readonly UpdateCompanyCommandHandler _updateHandler;
    private readonly DeactivateCompanyCommandHandler _deactivateHandler;
    private readonly ActivateCompanyCommandHandler _activateHandler;
    private readonly SetCompanyLicenseCommandHandler _setLicenseHandler;
    private readonly UpdateCompanyLogoCommandHandler _updateLogoHandler;
    private readonly ImportCompaniesCommandHandler _importHandler;
    private readonly IImportFileParser _importFileParser;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CompaniesController(
        ListCompaniesQueryHandler listHandler,
        GetCompanyByIdQueryHandler getByIdHandler,
        GetCompanyLogoQueryHandler getLogoHandler,
        CreateCompanyCommandHandler createHandler,
        UpdateCompanyCommandHandler updateHandler,
        DeactivateCompanyCommandHandler deactivateHandler,
        ActivateCompanyCommandHandler activateHandler,
        SetCompanyLicenseCommandHandler setLicenseHandler,
        UpdateCompanyLogoCommandHandler updateLogoHandler,
        ImportCompaniesCommandHandler importHandler,
        IImportFileParser importFileParser,
        IWebHostEnvironment webHostEnvironment)
    {
        _listHandler = listHandler;
        _getByIdHandler = getByIdHandler;
        _getLogoHandler = getLogoHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
        _activateHandler = activateHandler;
        _setLicenseHandler = setLicenseHandler;
        _updateLogoHandler = updateLogoHandler;
        _importHandler = importHandler;
        _importFileParser = importFileParser;
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

    /// <summary>Same filters as List, but every matching row in one file instead of one page.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search, [FromQuery] bool includeInactive, [FromQuery] string? statusFilter = null, CancellationToken cancellationToken = default)
    {
        // 5000 - matches the cap ListCompaniesQueryHandler itself clamps to.
        var result = await _listHandler.Handle(new ListCompaniesQuery(search, includeInactive, 1, 5000, statusFilter), GetCaller(), cancellationToken);
        var bytes = BuildExportFile(result.Items);
        return File(bytes, ExcelExportWriter.ContentType, "Companies.xlsx");
    }

    private static byte[] BuildExportFile(IReadOnlyCollection<CompanyListItemDto> items)
    {
        string[] headers =
        [
            "Company", "Company Code", "Email", "Phone", "Street 1", "Street 2", "City", "State", "Zip",
            "Payment Form", "Total Payment", "Plan", "Purchased", "Starts", "Expires", "Status",
        ];

        var rows = items.Select(c => (IReadOnlyList<string>)
        [
            c.CompanyName,
            c.CompanyCode,
            c.CompanyEmail ?? "",
            c.CompanyPhone ?? "",
            c.Street1 ?? "",
            c.Street2 ?? "",
            c.City ?? "",
            c.State ?? "",
            c.Zip ?? "",
            c.PaymentForm ?? "",
            c.TotalPayment?.ToString("0.00") ?? "",
            c.PlanType,
            c.PurchaseDate?.ToString("yyyy-MM-dd") ?? "",
            c.PlanStartDate.ToString("yyyy-MM-dd"),
            c.PlanEndDate.ToString("yyyy-MM-dd"),
            c.IsActive ? (c.IsExpired ? "Expired" : "Active") : "Deactivated",
        ]);

        return ExcelExportWriter.Write("Companies", headers, rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(id, GetCaller(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Any authenticated user (not SuperAdmin-only like GetById) - just the logo URL, no
    /// billing/address data, so a Manager/CompanyAdmin/Employee can show their own company's logo
    /// in the header without needing full company-record access.</summary>
    [HttpGet("{id:int}/logo")]
    public async Task<IActionResult> GetLogo(int id, CancellationToken cancellationToken)
    {
        var logoUrl = await _getLogoHandler.Handle(id, cancellationToken);
        return Ok(new { logoUrl });
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

    /// <summary>Clears a company's logo. Uploading could only ever REPLACE one, so a logo added by
    /// mistake could not be taken off again.
    ///
    /// The database row is cleared first and the file deleted afterwards, on purpose: if the delete
    /// fails, the company is already showing no logo and an orphaned file is harmless. Clearing the
    /// row only after a successful delete would leave the company pointing at a file that is gone.</summary>
    [HttpDelete("{id:int}/logo")]
    public async Task<IActionResult> RemoveLogo(int id, CancellationToken cancellationToken)
    {
        await _updateLogoHandler.Handle(id, logoUrl: null, GetCaller(), cancellationToken);

        var webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var logoDirectory = Path.Combine(webRootPath, "company-logos");

        if (Directory.Exists(logoDirectory))
        {
            // The extension depends on what was uploaded, so remove whichever "<id>.*" is there.
            foreach (var path in Directory.EnumerateFiles(logoDirectory, $"{id}.*"))
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (IOException)
                {
                    // A locked or already-removed file must not fail the request - the company has
                    // no logo either way, which is what was asked for.
                }
            }
        }

        return NoContent();
    }

    /// <summary>SuperAdmin only. Reads an .xlsx/.csv Company Import file and, for every row,
    /// creates or completes the matching Company + Company Admin - see ImportCompaniesCommandHandler
    /// for the full reconciliation logic. Returns a summary plus a per-row result so the admin can
    /// see exactly what happened to every row.</summary>
    [HttpPost("import")]
    [RequestSizeLimit(MaxImportFileSizeBytes)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > MaxImportFileSizeBytes)
        {
            return BadRequest(new { message = "Import file must be 10MB or smaller." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImportExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Only .xlsx and .csv files are supported." });
        }

        await using var stream = file.OpenReadStream();
        var rows = _importFileParser.Parse(stream, file.FileName);

        var result = await _importHandler.Handle(new ImportCompaniesCommand(rows), GetCaller(), cancellationToken);
        return Ok(result);
    }

    private CallerContext GetCaller() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        User.FindFirstValue(ClaimTypes.Email)!,
        User.FindFirstValue(ClaimTypes.Role)!);
}
