using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Managers.Commands.DeactivateManager;

public class DeactivateManagerCommandHandler
{
    private readonly IManagerRepository _repository;

    public DeactivateManagerCommandHandler(IManagerRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can deactivate a manager or company admin.");
        }

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        user.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
