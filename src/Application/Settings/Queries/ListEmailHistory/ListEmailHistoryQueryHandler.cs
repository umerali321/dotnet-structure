using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Settings.Queries.ListEmailHistory;

public class ListEmailHistoryQueryHandler
{
    private readonly IEmailLogRepository _repository;

    public ListEmailHistoryQueryHandler(IEmailLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<EmailLogDto>> Handle(ListEmailHistoryQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view Email History.");
        }

        return await _repository.ListAsync(query.Page, query.PageSize, query.Search, query.Purpose, cancellationToken);
    }
}
