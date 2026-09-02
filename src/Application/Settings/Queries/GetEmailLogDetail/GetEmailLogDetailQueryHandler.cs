using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetEmailLogDetail;

public class GetEmailLogDetailQueryHandler
{
    private readonly IEmailLogRepository _repository;
    private readonly IPermissionService _permissionService;

    public GetEmailLogDetailQueryHandler(IEmailLogRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<EmailLogDetailDto?> Handle(int emailLogId, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ViewEmailHistory, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view Email History.");
        }

        return await _repository.GetByIdAsync(emailLogId, cancellationToken);
    }
}
