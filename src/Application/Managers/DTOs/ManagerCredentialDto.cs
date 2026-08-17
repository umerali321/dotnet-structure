namespace SkillsetsBackend.Application.Managers.DTOs;

/// <summary>
/// Legacy plaintext credential lookup for admin-assisted password recovery. Only ever returned to
/// SuperAdmin or a Manager/CompanyAdmin who manages this manager - see StudentAuthorization.EnsureCanManageManagerAsync.
/// </summary>
public record ManagerCredentialDto(string? Username, string? Email, string? Password, bool IsActive);
