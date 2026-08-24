using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Scraper.DTOs;
using SkillsetsBackend.Application.Scraper.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Scraper.Commands.StartScraperRun;

/// <summary>SuperAdmin only. Categories are deliberately not validated against the scraper's own
/// category list - an unknown name just fails fast inside the Python script itself (visible in
/// the log tail, ends the run as Failed) instead of needing a second, C#-side copy of that list
/// kept in sync with the site's actual categories.</summary>
public class StartScraperRunCommandHandler
{
    private static readonly HashSet<string> ValidModes = new(StringComparer.OrdinalIgnoreCase) { "top", "sidebar", "both" };

    private readonly IScraperRunnerService _runner;

    public StartScraperRunCommandHandler(IScraperRunnerService runner)
    {
        _runner = runner;
    }

    public Task<ScraperRunStatusDto> Handle(StartScraperRunCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can run the course scraper.");
        }

        var errors = new List<ValidationFailure>();
        var categories = (command.Categories ?? [])
            .Select(c => c?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!)
            .ToList();

        if (categories.Count == 0)
        {
            errors.Add(new ValidationFailure(nameof(command.Categories), "At least one category is required."));
        }
        if (string.IsNullOrWhiteSpace(command.Mode) || !ValidModes.Contains(command.Mode))
        {
            errors.Add(new ValidationFailure(nameof(command.Mode), "Mode must be 'top', 'sidebar', or 'both'."));
        }
        if (command.Limit is <= 0)
        {
            errors.Add(new ValidationFailure(nameof(command.Limit), "Limit must be greater than zero when provided."));
        }
        if (errors.Count > 0)
        {
            throw new AppValidationException(errors);
        }

        // "ALL" always wins over any other selections made alongside it, rather than rejecting
        // the combination outright - matches how the frontend's multi-select clears everything
        // else the moment ALL is picked, so this is really just a defensive backstop.
        if (categories.Any(c => string.Equals(c, "ALL", StringComparison.OrdinalIgnoreCase)))
        {
            categories = ["ALL"];
        }

        var snapshot = _runner.StartRun(categories, command.Mode.Trim().ToLowerInvariant(), command.Limit, caller.Email);
        return Task.FromResult(ScraperRunStatusDto.FromSnapshot(snapshot));
    }
}
