using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.CourseLibrary.Commands.MarkCourseTakenComplete;

public class MarkCourseTakenCompleteCommandHandler
{
    private readonly ICourseTakenRepository _repository;

    public MarkCourseTakenCompleteCommandHandler(ICourseTakenRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(MarkCourseTakenCompleteCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(command.CourseTakenId, cancellationToken)
            ?? throw new NotFoundException("CourseTaken", command.CourseTakenId);

        // Only the student who owns this record can mark it complete - not their manager, not
        // SuperAdmin, matching the "student self-marks complete" decision.
        if (caller.DbUserId != record.UserId)
        {
            throw new UnauthorizedAccessException("You can only complete your own course.");
        }

        if (record.IsActive)
        {
            record.MarkComplete();
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}
