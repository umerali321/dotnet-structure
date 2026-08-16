using FluentValidation;

namespace SkillsetsBackend.Application.CourseLibrary.Commands.TakeCourse;

public class TakeCourseCommandValidator : AbstractValidator<TakeCourseCommand>
{
    public TakeCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
    }
}
