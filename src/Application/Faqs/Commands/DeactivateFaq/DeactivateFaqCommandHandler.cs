using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Faqs.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.Faqs.Commands.DeactivateFaq;

public class DeactivateFaqCommandHandler
{
    private readonly IFaqRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public DeactivateFaqCommandHandler(IFaqRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int faqId, CallerContext caller, CancellationToken cancellationToken)
    {
        var faq = await _repository.GetEntityAsync(faqId, cancellationToken)
            ?? throw new NotFoundException(nameof(Faq), faqId);

        await FaqAuthorization.EnsureCanManageFaqAsync(caller, faq, _userDirectory, cancellationToken);

        faq.Deactivate(caller.Email);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
