using FluentValidation;
using SkillsetsBackend.Domain.Communications;

namespace SkillsetsBackend.Application.Settings.Commands.SaveSmtpSettings;

public class SaveSmtpSettingsCommandValidator : AbstractValidator<SaveSmtpSettingsCommand>
{
    public SaveSmtpSettingsCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().Must(SmtpProviderType.IsKnown)
            .WithMessage($"Provider must be one of: {string.Join(", ", SmtpProviderType.All)}.");
        RuleFor(x => x.Host).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Password).MaximumLength(500);
        RuleFor(x => x.FromEmail).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.FromName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ReplyToEmail).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.ReplyToEmail));
        RuleFor(x => x.SupportToEmail).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.SupportToEmail));
        RuleFor(x => x.SupportToName).MaximumLength(255);
    }
}
