using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RosterImport.Queries.GetCreationSourceStats;

/// <summary>
/// "How many employees/managers were created manually vs by roster import" - read straight off the
/// stored CreationSource columns, which is the whole reason they exist. Gated by Students.View
/// rather than the import permission: this is a reporting question, not an import action.
/// </summary>
public class GetCreationSourceStatsQueryHandler
{
    private readonly IRosterImportRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IUserDirectory _userDirectory;

    public GetCreationSourceStatsQueryHandler(
        IRosterImportRepository repository,
        IPermissionService permissionService,
        IUserDirectory userDirectory)
    {
        _repository = repository;
        _permissionService = permissionService;
        _userDirectory = userDirectory;
    }

    public async Task<CreationSourceStatsDto> Handle(int? companyId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin
            && !await _permissionService.HasPermissionAsync(caller, Permissions.Students.View, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view employee statistics.");
        }

        IReadOnlyCollection<int>? companyIds;
        if (caller.IsPlatformAdmin)
        {
            companyIds = companyId is null ? null : [companyId.Value];
        }
        else
        {
            var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
            if (companyId is not null && !managed.Contains(companyId.Value))
            {
                throw new UnauthorizedAccessException("You do not have access to that company.");
            }

            companyIds = companyId is null ? managed : [companyId.Value];
        }

        return await _repository.GetCreationSourceStatsAsync(companyIds, cancellationToken);
    }
}
