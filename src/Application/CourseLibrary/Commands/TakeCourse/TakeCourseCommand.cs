namespace SkillsetsBackend.Application.CourseLibrary.Commands.TakeCourse;

public record TakeCourseCommand(long CourseId, bool ConfirmRetake = false);
