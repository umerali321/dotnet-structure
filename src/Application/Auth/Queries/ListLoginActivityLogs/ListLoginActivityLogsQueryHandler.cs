using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Auth.Queries.ListLoginActivityLogs;

public class ListLoginActivityLogsQueryHandler
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private readonly ILoginActivityLogRepository _repository;

    public ListLoginActivityLogsQueryHandler(ILoginActivityLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListLoginActivityLogsResult> Handle(ListLoginActivityLogsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view system logs.");
        }

        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(MaxPageSize, query.PageSize);

        var logs = await _repository.ListAsync(
            page, pageSize, query.EventType, query.Email, query.Name, query.CompanyName, query.StartDate, query.EndDate, cancellationToken);
        var summary = await _repository.GetSummaryAsync(cancellationToken);

        return new ListLoginActivityLogsResult(logs, summary);
    }
}
