using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Faqs.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.Faqs.Commands.UpdateFaq;

public class UpdateFaqCommandHandler
{
    private readonly IValidator<UpdateFaqCommand> _validator;
    private readonly IFaqRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public UpdateFaqCommandHandler(IValidator<UpdateFaqCommand> validator, IFaqRepository repository, IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int faqId, UpdateFaqCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var faq = await _repository.GetEntityAsync(faqId, cancellationToken)
            ?? throw new NotFoundException(nameof(Faq), faqId);

        await FaqAuthorization.EnsureCanManageFaqAsync(caller, faq, _userDirectory, cancellationToken);

        faq.Update(command.Question, command.Answer, caller.Email);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
