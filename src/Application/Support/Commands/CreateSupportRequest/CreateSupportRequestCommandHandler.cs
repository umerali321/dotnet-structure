using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Support.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Domain.Support;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using FluentValidation.Results;

namespace SkillsetsBackend.Application.Support.Commands.CreateSupportRequest;

public class CreateSupportRequestCommandHandler
{
    private readonly IValidator<CreateSupportRequestCommand> _validator;
    private readonly ISupportRequestRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public CreateSupportRequestCommandHandler(
        IValidator<CreateSupportRequestCommand> validator,
        ISupportRequestRepository repository,
        IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<int> Handle(CreateSupportRequestCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can submit support requests.");
        }

        if (caller.DbUserId is null)
        {
            throw new UnauthorizedAccessException("Your account could not be identified.");
        }

        int companyId;

        if (caller.IsSuperAdmin)
        {
            if (command.CompanyId is null)
            {
                throw new AppValidationException([new ValidationFailure(nameof(command.CompanyId), "Select a company for this support request.")]);
            }

            companyId = command.CompanyId.Value;
        }
        else
        {
            var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);

            if (command.CompanyId is not null)
            {
                if (!managed.Contains(command.CompanyId.Value))
                {
                    throw new UnauthorizedAccessException("You do not have access to that company.");
                }

                companyId = command.CompanyId.Value;
            }
            else if (managed.Count == 1)
            {
                companyId = managed.Single();
            }
            else
            {
                throw new AppValidationException([new ValidationFailure(nameof(command.CompanyId), "Select which of your companies this request is for.")]);
            }
        }

        var request = SupportRequest.Create(companyId, caller.DbUserId.Value, command.Subject, command.Message);
        await _repository.AddAsync(request, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return request.SupportRequestId;
    }
}
