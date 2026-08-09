using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Support.DTOs;
using SkillsetsBackend.Application.Support.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Support.Queries.ListSupportRequests;

/// <summary>
/// SuperAdmin sees every request platform-wide, with optional company/status filters. A Manager
/// only ever sees the requests they personally submitted - there is no "view my company's tickets"
/// capability per the product spec, just "submit one for my company" and track its status.
/// </summary>
public class ListSupportRequestsQueryHandler
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private readonly ISupportRequestRepository _repository;

    public ListSupportRequestsQueryHandler(ISupportRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<SupportRequestDto>> Handle(ListSupportRequestsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can view support requests.");
        }

        var options = new SupportRequestListQueryOptions(
            Math.Max(1, query.Page),
            query.PageSize <= 0 ? DefaultPageSize : Math.Min(MaxPageSize, query.PageSize),
            caller.IsSuperAdmin ? query.CompanyId : null,
            query.Status,
            null,
            caller.IsSuperAdmin ? null : caller.DbUserId);

        return await _repository.ListAsync(options, cancellationToken);
    }
}
