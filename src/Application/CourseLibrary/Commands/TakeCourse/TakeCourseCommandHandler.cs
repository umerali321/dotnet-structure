using FluentValidation;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using ConflictException = SkillsetsBackend.Application.Common.Exceptions.ConflictException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.CourseLibrary.Commands.TakeCourse;

public class TakeCourseCommandHandler
{
    private readonly IValidator<TakeCourseCommand> _validator;
    private readonly ICourseTakenRepository _repository;
    private readonly ICourseLibraryQueryService _courseLibraryQueryService;

    public TakeCourseCommandHandler(
        IValidator<TakeCourseCommand> validator,
        ICourseTakenRepository repository,
        ICourseLibraryQueryService courseLibraryQueryService)
    {
        _validator = validator;
        _repository = repository;
        _courseLibraryQueryService = courseLibraryQueryService;
    }

    public async Task<TakeCourseResultDto> Handle(TakeCourseCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (caller.Role != Roles.Student || caller.DbUserId is null)
        {
            throw new UnauthorizedAccessException("Only students can take a course.");
        }

        var userId = caller.DbUserId.Value;

        _ = await _courseLibraryQueryService.GetCourseDetailAsync(command.CourseId, cancellationToken)
            ?? throw new NotFoundException("Course", command.CourseId);

        var existing = await _repository.FindByUserAndCourseAsync(userId, command.CourseId, cancellationToken);
        if (existing is not null)
        {
            if (existing.IsActive)
            {
                // Idempotent - re-clicking Launch/Take Course on your own current course just
                // re-opens it, it does not create another record.
                var dto = await _repository.GetDtoAsync(existing.CourseTakenId, cancellationToken);
                return new TakeCourseResultDto(dto, RequiresConfirmation: false, ConfirmationMessage: null);
            }

            if (!command.ConfirmRetake)
            {
                return new TakeCourseResultDto(
                    CourseTaken: null,
                    RequiresConfirmation: true,
                    ConfirmationMessage: "You have already completed this course. Are you sure you want to take it again?");
            }

            // Confirmed retake - fall through and create a new CourseTaken row below, preserving
            // the prior completed record's history instead of reactivating it in place.
        }

        // A student may have as many courses in progress at once as they like.
        //
        // The old rule ("finish your current course before starting another") was removed at the
        // customer's request, for two reasons that make it unworkable rather than merely strict:
        //   1. Completion comes from the Skillport usage report, which can lag by up to two days.
        //      A course the student genuinely finished still reads "In Progress" until the next
        //      import, so the rule blocked people on the strength of stale data.
        //   2. Nothing about the 30-day session is meant to limit how many courses run at once.
        //
        // Removing this also removed the need for the "Cancel Current Lab / Continue Current Lab"
        // prompt - there is no longer a current lab to cancel. CancelActive is still accepted on the
        // command so existing callers keep working; it simply has nothing left to do.
        var courseTaken = CourseTaken.Create(userId, command.CourseId);

        var added = await _repository.TryAddAsync(courseTaken, cancellationToken);
        if (!added)
        {
            // No longer about an active course - the only way this fails now is a genuine
            // concurrent double-submit of the SAME course by the same student.
            throw new ConflictException("This course could not be started. Please try again.");
        }

        var createdDto = await _repository.GetDtoAsync(courseTaken.CourseTakenId, cancellationToken);
        return new TakeCourseResultDto(createdDto, RequiresConfirmation: false, ConfirmationMessage: null);
    }
}
