using FluentValidation;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Support.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.Support.Commands.UpdateSupportRequestStatus;

public class UpdateSupportRequestStatusCommandHandler
{
    private readonly IValidator<UpdateSupportRequestStatusCommand> _validator;
    private readonly ISupportRequestRepository _repository;

    public UpdateSupportRequestStatusCommandHandler(IValidator<UpdateSupportRequestStatusCommand> validator, ISupportRequestRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task Handle(int supportRequestId, UpdateSupportRequestStatusCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can update a support request's status.");
        }

        var request = await _repository.GetEntityAsync(supportRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(SupportRequest), supportRequestId);

        request.UpdateStatus(command.Status);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
