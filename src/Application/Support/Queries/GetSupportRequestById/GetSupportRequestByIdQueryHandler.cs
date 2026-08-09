using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Support.DTOs;
using SkillsetsBackend.Application.Support.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Support.Queries.GetSupportRequestById;

public class GetSupportRequestByIdQueryHandler
{
    private readonly ISupportRequestRepository _repository;

    public GetSupportRequestByIdQueryHandler(ISupportRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<SupportRequestDto?> Handle(int supportRequestId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can view support requests.");
        }

        var request = await _repository.GetDtoAsync(supportRequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        if (!caller.IsSuperAdmin && request.UserId != caller.DbUserId)
        {
            throw new UnauthorizedAccessException("You do not have access to this support request.");
        }

        return request;
    }
}
