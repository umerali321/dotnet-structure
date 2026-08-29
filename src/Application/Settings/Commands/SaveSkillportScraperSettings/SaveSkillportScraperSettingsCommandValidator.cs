using FluentValidation;

namespace SkillsetsBackend.Application.Settings.Commands.SaveSkillportScraperSettings;

public class SaveSkillportScraperSettingsCommandValidator : AbstractValidator<SaveSkillportScraperSettingsCommand>
{
    public SaveSkillportScraperSettingsCommandValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(200);
    }
}
