namespace SkillsetsBackend.Application.CourseLibrary.Commands.TakeCourse;

/// <param name="CancelActive">Set once the student has confirmed "cancel my current course and
/// start this one" in the active-course dialog. Without it a student who already has an active
/// course gets the 409 that drives that dialog, since only one course may be active at a time.</param>
public record TakeCourseCommand(long CourseId, bool ConfirmRetake = false, bool CancelActive = false);
