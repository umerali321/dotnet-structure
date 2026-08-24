namespace SkillsetsBackend.Application.SkillTrax.Commands.UpdateSkillTrax;

public record UpdateSkillTraxCommand(string Name, IReadOnlyList<long> CourseIds);
