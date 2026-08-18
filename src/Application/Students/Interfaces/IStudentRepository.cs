using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students.Interfaces;

public interface IStudentRepository
{
    Task<AppUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<StudentProfile?> GetProfileByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Cheap projection for authorization checks - null if unassigned (or no profile row exists).</summary>
    Task<int?> GetManagerIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> IdentifierInUseAsync(string email, string username, int? excludeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the Users row, then the StudentProfile and UserCompanyRole rows referencing the
    /// newly generated UserId, atomically. Returns the new UserId.
    /// </summary>
    Task<int> CreateStudentAsync(
        AppUser user,
        string? studentType,
        string createdBy,
        int companyId,
        DateOnly? startDate,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
