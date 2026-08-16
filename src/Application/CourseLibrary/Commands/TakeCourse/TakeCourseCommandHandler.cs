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

    public async Task<CourseTakenDto> Handle(TakeCourseCommand command, CallerContext caller, CancellationToken cancellationToken)
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
                return await _repository.GetDtoAsync(existing.CourseTakenId, cancellationToken);
            }

            throw new ConflictException("You have already completed this course.");
        }

        var activeForUser = await _repository.FindActiveByUserAsync(userId, cancellationToken);
        if (activeForUser is not null)
        {
            var activeCourse = await _courseLibraryQueryService.GetCourseDetailAsync(activeForUser.CourseId, cancellationToken);
            var activeTitle = activeCourse?.CourseTitle ?? "another course";
            throw new ConflictException($"You already have an active course: {activeTitle}. Complete it before starting a new one.");
        }

        var activeForCourse = await _repository.FindActiveByCourseAsync(command.CourseId, cancellationToken);
        if (activeForCourse is not null)
        {
            throw new ConflictException("This course is currently taken by another student.");
        }

        var courseTaken = CourseTaken.Create(userId, command.CourseId);

        var added = await _repository.TryAddAsync(courseTaken, cancellationToken);
        if (!added)
        {
            // A concurrent request won the race - the filtered unique indexes are the real
            // guarantee here, the checks above are just for a fast, friendly error message.
            throw new ConflictException("This course could not be started - it may already be active for you or taken by another student.");
        }

        return await _repository.GetDtoAsync(courseTaken.CourseTakenId, cancellationToken);
    }
}
