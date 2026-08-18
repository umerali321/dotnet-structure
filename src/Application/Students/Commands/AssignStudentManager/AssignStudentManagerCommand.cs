namespace SkillsetsBackend.Application.Students.Commands.AssignStudentManager;

/// <summary>ManagerId null clears the assignment, restoring today's company-wide Manager visibility.</summary>
public record AssignStudentManagerCommand(int? ManagerId);
