using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Faqs.DTOs;
using SkillsetsBackend.Application.Faqs.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Faqs.Queries.GetFaqById;

public class GetFaqByIdQueryHandler
{
    private readonly IFaqRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public GetFaqByIdQueryHandler(IFaqRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<FaqDto?> Handle(int faqId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can manage FAQs.");
        }

        var faq = await _repository.GetDtoAsync(faqId, cancellationToken);
        if (faq is null)
        {
            return null;
        }

        if (!caller.IsSuperAdmin)
        {
            if (faq.CompanyId is null)
            {
                throw new UnauthorizedAccessException("You do not have access to this FAQ.");
            }

            var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
            if (!managed.Contains(faq.CompanyId.Value))
            {
                throw new UnauthorizedAccessException("You do not have access to this FAQ.");
            }
        }

        return faq;
    }
}
