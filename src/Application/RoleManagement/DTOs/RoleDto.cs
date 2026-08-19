namespace SkillsetsBackend.Application.RoleManagement.DTOs;

public record RoleDto(byte RoleId, string RoleName, bool IsSystemRole, bool IsActive, IReadOnlyList<PermissionDto> Permissions);

public record RoleSummaryDto(byte RoleId, string RoleName, bool IsSystemRole, bool IsActive);
