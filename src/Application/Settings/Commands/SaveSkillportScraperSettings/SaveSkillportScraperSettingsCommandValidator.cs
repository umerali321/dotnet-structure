using FluentValidation;
using SkillsetsBackend.Domain.Skillsoft;

namespace SkillsetsBackend.Application.Settings.Commands.SaveSkillportScraperSettings;

public class SaveSkillportScraperSettingsCommandValidator : AbstractValidator<SaveSkillportScraperSettingsCommand>
{
    public SaveSkillportScraperSettingsCommandValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(200);

        RuleFor(x => x.DateRangeMode)
            .Must(mode => SkillportScraperDateRangeModes.All.Contains(mode))
            .WithMessage($"DateRangeMode must be one of: {string.Join(", ", SkillportScraperDateRangeModes.All)}.");

        RuleFor(x => x.CustomDateFrom)
            .NotNull().WithMessage("A start date is required for a custom date range.")
            .When(x => x.DateRangeMode == SkillportScraperDateRangeModes.Custom);

        RuleFor(x => x.CustomDateTo)
            .NotNull().WithMessage("An end date is required for a custom date range.")
            .When(x => x.DateRangeMode == SkillportScraperDateRangeModes.Custom);

        RuleFor(x => x)
            .Must(x => x.CustomDateFrom is null || x.CustomDateTo is null || x.CustomDateFrom <= x.CustomDateTo)
            .WithMessage("The start date must be on or before the end date.")
            .When(x => x.DateRangeMode == SkillportScraperDateRangeModes.Custom);
    }
}
