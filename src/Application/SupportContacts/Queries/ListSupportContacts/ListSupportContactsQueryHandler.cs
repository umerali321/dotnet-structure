using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SupportContacts.DTOs;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.SupportContacts.Queries.ListSupportContacts;

public class ListSupportContactsQueryHandler
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private readonly ISupportContactRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public ListSupportContactsQueryHandler(ISupportContactRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<PaginatedList<SupportContactDto>> Handle(ListSupportContactsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<int>? restrictToCompanyIds;

        if (caller.IsPlatformAdmin)
        {
            restrictToCompanyIds = query.CompanyId is null ? null : [query.CompanyId.Value];
        }
        else
        {
            // Managers manage a set of companies; students/other roles can only read contacts for the
            // companies they actively belong to (regardless of their role in that company).
            var accessible = caller.Role == Roles.Manager
                ? await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken)
                : (caller.DbUserId is null
                    ? []
                    : (await _userDirectory.GetActiveCompanyRolesAsync(caller.DbUserId.Value, cancellationToken))
                        .Select(r => r.CompanyId)
                        .Distinct()
                        .ToList());

            if (query.CompanyId is not null && !accessible.Contains(query.CompanyId.Value))
            {
                throw new UnauthorizedAccessException("You do not have access to that company.");
            }

            restrictToCompanyIds = query.CompanyId is null ? accessible : [query.CompanyId.Value];
        }

        var options = new SupportContactListQueryOptions(
            Math.Max(1, query.Page),
            query.PageSize <= 0 ? DefaultPageSize : Math.Min(MaxPageSize, query.PageSize),
            query.CompanyId,
            query.IsActive,
            restrictToCompanyIds);

        return await _repository.ListAsync(options, cancellationToken);
    }
}
