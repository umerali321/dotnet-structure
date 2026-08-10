using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

public interface ISkillsoftSsoService
{
    Task<string> CreateLaunchTicketAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default);

    Task<SkillsoftLaunchResult> ConsumeLaunchTicketAsync(string ticket, CancellationToken cancellationToken = default);
}

public record SkillsoftLaunchResult(string RedirectUrl);
