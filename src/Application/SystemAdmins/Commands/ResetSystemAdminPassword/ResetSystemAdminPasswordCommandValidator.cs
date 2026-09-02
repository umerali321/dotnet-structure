using FluentValidation;

namespace SkillsetsBackend.Application.SystemAdmins.Commands.ResetSystemAdminPassword;

public class ResetSystemAdminPasswordCommandValidator : AbstractValidator<ResetSystemAdminPasswordCommand>
{
    public ResetSystemAdminPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(4).MaximumLength(500);

        // Checked server-side as well as in the form: the confirmation exists to catch a typo, and a
        // typo is just as possible from anything else calling this endpoint.
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("The two passwords do not match.");
    }
}
