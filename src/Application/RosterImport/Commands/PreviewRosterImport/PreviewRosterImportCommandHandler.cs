using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RosterImport.Commands.PreviewRosterImport;

public record PreviewRosterImportCommand(Stream FileStream, string FileName, int? CompanyId);

/// <summary>
/// Step 2 of the wizard: says exactly what the import would do, and writes nothing. Runs the same
/// planner the real import runs, so "3 will be created, 2 skipped" is a promise rather than an
/// estimate.
/// </summary>
public class PreviewRosterImportCommandHandler
{
    private readonly IRosterImportFileParser _parser;
    private readonly RosterImportPlanner _planner;
    private readonly IPermissionService _permissionService;
    private readonly IUserDirectory _userDirectory;

    public PreviewRosterImportCommandHandler(
        IRosterImportFileParser parser,
        RosterImportPlanner planner,
        IPermissionService permissionService,
        IUserDirectory userDirectory)
    {
        _parser = parser;
        _planner = planner;
        _permissionService = permissionService;
        _userDirectory = userDirectory;
    }

    public async Task<RosterImportPreviewDto> Handle(
        PreviewRosterImportCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var callerCompanyIds = await RosterImportAuthorization.AuthorizeAsync(
            caller, _permissionService, _userDirectory, cancellationToken);

        var parsed = _parser.Parse(command.FileStream, command.FileName);
        var plan = await _planner.BuildAsync(
            parsed, command.FileName, command.CompanyId, callerCompanyIds, cancellationToken);

        var rows = plan.Rows
            .Select(p => new RosterRowResultDto(
                p.Row.RowNumber,
                p.Row.DisplayName,
                p.Row.Email,
                p.CompanyName,
                p.Row.EmployeeType,
                p.Row.GiveManagerDashboard,
                // Future tense: nothing has happened yet, and calling it "Created" in a dry run
                // would be a lie the admin acts on.
                p.Action switch
                {
                    RosterRowAction.CreateUser => "Will create",
                    RosterRowAction.GrantManagerToExistingUser => "Will update",
                    RosterRowAction.Skip => "Will skip",
                    _ => "Invalid",
                },
                p.Reason))
            .ToList();

        return new RosterImportPreviewDto(plan.FileInfo, plan.Summary, rows);
    }
}

/// <summary>Shared gate for every roster-import endpoint: the grantable Students.Import permission,
/// plus the caller's company scope. A Manager or Company Admin who has been given the permission can
/// import - but only into companies they actually manage.</summary>
public static class RosterImportAuthorization
{
    /// <summary>Returns the company ids the caller may import into, or null for an unrestricted
    /// platform admin.</summary>
    public static async Task<IReadOnlyCollection<int>?> AuthorizeAsync(
        CallerContext caller,
        IPermissionService permissionService,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin
            && !await permissionService.HasPermissionAsync(caller, Permissions.Students.Import, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to import an employee roster.");
        }

        if (caller.IsPlatformAdmin)
        {
            return null;
        }

        return await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
    }
}
