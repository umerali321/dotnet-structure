using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Domain.Support;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.SupportContacts.Commands.CreateSupportContact;

public class CreateSupportContactCommandHandler
{
    private readonly IValidator<CreateSupportContactCommand> _validator;
    private readonly ISupportContactRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public CreateSupportContactCommandHandler(IValidator<CreateSupportContactCommand> validator, ISupportContactRepository repository, IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<int> Handle(CreateSupportContactCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsPlatformAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can create contacts.");
        }

        if (!caller.IsPlatformAdmin)
        {
            if (command.CompanyId is null)
            {
                throw new UnauthorizedAccessException("A company must be selected for this contact.");
            }

            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId.Value, _userDirectory, cancellationToken);
        }

        var contact = SupportContact.Create(command.CompanyId, command.ContactType, command.Value, command.SortOrder, caller.Email);
        await _repository.AddAsync(contact, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return contact.SupportContactId;
    }
}
