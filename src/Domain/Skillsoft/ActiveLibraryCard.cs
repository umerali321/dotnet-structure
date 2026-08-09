using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Skillsoft;

public class ActiveLibraryCard : IAggregateRoot
{
    public string CompanyCode { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? ManagerId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string UserId { get; private set; } = string.Empty;

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public string FdmName { get; private set; } = string.Empty;

    private ActiveLibraryCard()
    {
    }
}
