using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Domain.Support;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.SupportContacts.Commands.UpdateSupportContact;

public class UpdateSupportContactCommandHandler
{
    private readonly IValidator<UpdateSupportContactCommand> _validator;
    private readonly ISupportContactRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public UpdateSupportContactCommandHandler(IValidator<UpdateSupportContactCommand> validator, ISupportContactRepository repository, IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int supportContactId, UpdateSupportContactCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var contact = await _repository.GetEntityAsync(supportContactId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportContact), supportContactId);

        await SupportContactAuthorization.EnsureCanManageContactAsync(caller, contact, _userDirectory, cancellationToken);

        contact.Update(command.ContactType, command.Value, command.SortOrder, caller.Email);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
