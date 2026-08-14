using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Skillsoft;

public class ActiveLibraryCard : IAggregateRoot
{
    /// <summary>
    /// Surrogate key added specifically so this table has one - the legacy data has no natural unique
    /// key (rows can be exact duplicates except for FDM_Name). Additive only: existing rows keep their
    /// data, this is just a new identity column appended to the table.
    /// </summary>
    public int ActiveLibraryCardId { get; private set; }

    public string CompanyCode { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? ManagerId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string UserId { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public string FdmName { get; private set; } = string.Empty;

    private ActiveLibraryCard()
    {
    }
}
