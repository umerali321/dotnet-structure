namespace SkillsetsBackend.Application.SkillTrax.Commands.CreateSkillTrax;

/// <summary>ActingAsUserId is required only when the caller is SuperAdmin (a real Manager/Company
/// Admin at CompanyId to create this on behalf of - see ActingAsResolver); ignored otherwise.</summary>
public record CreateSkillTraxCommand(int CompanyId, string Name, IReadOnlyList<long> CourseIds, int? ActingAsUserId = null);
