using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Faqs.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Domain.Support;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Faqs.Commands.CreateFaq;

public class CreateFaqCommandHandler
{
    private readonly IValidator<CreateFaqCommand> _validator;
    private readonly IFaqRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public CreateFaqCommandHandler(IValidator<CreateFaqCommand> validator, IFaqRepository repository, IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<int> Handle(CreateFaqCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can create FAQs.");
        }

        if (caller.IsSuperAdmin)
        {
            // SuperAdmin may create a company-scoped FAQ or, by leaving CompanyId out, a global one.
        }
        else
        {
            if (command.CompanyId is null)
            {
                throw new UnauthorizedAccessException("A company must be selected for this FAQ.");
            }

            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId.Value, _userDirectory, cancellationToken);
        }

        var faq = Faq.Create(command.CompanyId, command.Question, command.Answer, caller.Email);
        await _repository.AddAsync(faq, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return faq.FaqId;
    }
}
