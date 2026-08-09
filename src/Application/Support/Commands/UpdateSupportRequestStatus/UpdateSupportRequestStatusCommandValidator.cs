using FluentValidation;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.Support.Commands.UpdateSupportRequestStatus;

public class UpdateSupportRequestStatusCommandValidator : AbstractValidator<UpdateSupportRequestStatusCommand>
{
    public UpdateSupportRequestStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(SupportRequestStatus.IsValid)
            .WithMessage($"Status must be one of: {string.Join(", ", SupportRequestStatus.All)}.");
    }
}
