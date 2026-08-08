using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Shared.Common;
namespace SkillsetsBackend.Application.Managers.Interfaces;
public record ManagerListQueryOptions(int Page,int PageSize,string? Search,bool? IsActive,string? SortBy,bool SortDescending,IReadOnlyCollection<int>? RestrictToCompanyIds);
public interface IManagerQueryService { Task<PaginatedList<ManagerListItemDto>> ListAsync(ManagerListQueryOptions options,CancellationToken cancellationToken=default); Task<ManagerListItemDto?> GetAsync(int userId,CancellationToken cancellationToken=default); }
