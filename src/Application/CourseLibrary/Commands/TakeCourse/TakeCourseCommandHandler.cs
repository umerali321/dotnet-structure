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

        var activeForUser = await _repository.FindActiveByUserAsync(userId, cancellationToken);
        if (activeForUser is not null)
        {
            if (!command.CancelActive)
            {
                var activeCourse = await _courseLibraryQueryService.GetCourseDetailAsync(activeForUser.CourseId, cancellationToken);
                var activeTitle = activeCourse?.CourseTitle ?? "another course";
                throw new ConflictException($"You already have an active course: {activeTitle}. Complete it before starting a new one.");
            }

            // The student chose "cancel my current course and start this one" and confirmed it.
            // Cancelled, not completed - see CourseTaken.Cancel. This also has to clear before the
            // insert below, or the filtered unique index on "one active row per user" rejects it.
            activeForUser.Cancel();
            await _repository.SaveChangesAsync(cancellationToken);
        }

        var courseTaken = CourseTaken.Create(userId, command.CourseId);

        var added = await _repository.TryAddAsync(courseTaken, cancellationToken);
        if (!added)
        {
            // A concurrent request won the race - the filtered unique index is the real
            // guarantee here, the check above is just for a fast, friendly error message.
            throw new ConflictException("This course could not be started - you may already have an active course.");
        }

        var createdDto = await _repository.GetDtoAsync(courseTaken.CourseTakenId, cancellationToken);
        return new TakeCourseResultDto(createdDto, RequiresConfirmation: false, ConfirmationMessage: null);
    }
}
