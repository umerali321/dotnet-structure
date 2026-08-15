using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Auth;

public class UserCredentialRepository : IUserCredentialRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserCredentialRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
