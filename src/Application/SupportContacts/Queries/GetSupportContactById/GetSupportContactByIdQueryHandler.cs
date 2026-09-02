using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SupportContacts.DTOs;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.SupportContacts.Queries.GetSupportContactById;

public class GetSupportContactByIdQueryHandler
{
    private readonly ISupportContactRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public GetSupportContactByIdQueryHandler(ISupportContactRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<SupportContactDto?> Handle(int supportContactId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsPlatformAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can manage contacts.");
        }

        var contact = await _repository.GetDtoAsync(supportContactId, cancellationToken);
        if (contact is null)
        {
            return null;
        }

        if (!caller.IsPlatformAdmin)
        {
            if (contact.CompanyId is null)
            {
                throw new UnauthorizedAccessException("You do not have access to this contact.");
            }

            var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
            if (!managed.Contains(contact.CompanyId.Value))
            {
                throw new UnauthorizedAccessException("You do not have access to this contact.");
            }
        }

        return contact;
    }
}
