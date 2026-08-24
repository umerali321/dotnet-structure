using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Assignments.Queries.ListMyAssignments;

/// <summary>Employee "My Assignments" view - always the caller's own assignments, no scope check
/// needed since it can never reveal anyone else's data.</summary>
public class ListMyAssignmentsQueryHandler
{
    private readonly IAssignmentQueryService _queryService;

    public ListMyAssignmentsQueryHandler(IAssignmentQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<AssignmentDto>> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (caller.DbUserId is null)
        {
            return [];
        }

        return await _queryService.ListMineAsync(caller.DbUserId.Value, cancellationToken);
    }
}
