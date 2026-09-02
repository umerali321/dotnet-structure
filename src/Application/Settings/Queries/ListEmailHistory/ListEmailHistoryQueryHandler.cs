using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Settings.Queries.ListEmailHistory;

public class ListEmailHistoryQueryHandler
{
    private readonly IEmailLogRepository _repository;
    private readonly IPermissionService _permissionService;

    public ListEmailHistoryQueryHandler(IEmailLogRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<PaginatedList<EmailLogDto>> Handle(ListEmailHistoryQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ViewEmailHistory, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view Email History.");
        }

        return await _repository.ListAsync(query.Page, query.PageSize, query.Search, query.Purpose, cancellationToken);
    }
}
