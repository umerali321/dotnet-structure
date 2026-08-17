namespace SkillsetsBackend.Application.Students.DTOs;

/// <summary>
/// Legacy plaintext credential lookup for admin-assisted password recovery. Only ever returned to
/// SuperAdmin or a Manager who manages this student - see StudentAuthorization.EnsureCanManageStudentAsync.
/// </summary>
public record StudentCredentialDto(string? Username, string? Email, string? Password, bool IsActive);
