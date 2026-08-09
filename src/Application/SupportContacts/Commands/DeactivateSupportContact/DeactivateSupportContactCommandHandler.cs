using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Domain.Support;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.SupportContacts.Commands.DeactivateSupportContact;

public class DeactivateSupportContactCommandHandler
{
    private readonly ISupportContactRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public DeactivateSupportContactCommandHandler(ISupportContactRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int supportContactId, CallerContext caller, CancellationToken cancellationToken)
    {
        var contact = await _repository.GetEntityAsync(supportContactId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportContact), supportContactId);

        await SupportContactAuthorization.EnsureCanManageContactAsync(caller, contact, _userDirectory, cancellationToken);

        contact.Deactivate(caller.Email);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
