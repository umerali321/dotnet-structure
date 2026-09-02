using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Shared.Common;
namespace SkillsetsBackend.Application.Managers.Interfaces;
/// <summary>RoleFilter is null for today's default (Manager/Admin) or "CompanyAdmin" to list company
/// admins instead - the same Users/UserCompanyRoles data, just a different RoleName.
/// Search targets ONE named field (see SearchCriteria); null means no search.</summary>
public record ManagerListQueryOptions(int Page,int PageSize,SearchCriteria? Search,bool? IsActive,string? SortBy,bool SortDescending,IReadOnlyCollection<int>? RestrictToCompanyIds,string? RoleFilter = null);
public interface IManagerQueryService { Task<PaginatedList<ManagerListItemDto>> ListAsync(ManagerListQueryOptions options,CancellationToken cancellationToken=default); Task<ManagerListItemDto?> GetAsync(int userId,CancellationToken cancellationToken=default); }
