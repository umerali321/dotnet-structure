using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetEmailLogDetail;

public class GetEmailLogDetailQueryHandler
{
    private readonly IEmailLogRepository _repository;

    public GetEmailLogDetailQueryHandler(IEmailLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<EmailLogDetailDto?> Handle(int emailLogId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view Email History.");
        }

        return await _repository.GetByIdAsync(emailLogId, cancellationToken);
    }
}
