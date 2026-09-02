using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.RosterImport;

/// <summary>
/// One run of the Employee Roster Import. Persisted rather than kept in memory for three reasons:
/// the results table has to survive a page refresh, the admin can re-download the results later,
/// and - most importantly - welcome emails are sent AFTER the import as a separate confirmed step,
/// so the set of accounts a batch created has to still be knowable when that step runs.
///
/// Note what is deliberately NOT stored here: the assigned passwords. They already live in
/// Users.PasswordHash (which, in this legacy schema, is the plaintext value - see AppUser), so the
/// welcome-email step reads them back from there rather than keeping a second copy.
/// </summary>
public class RosterImportBatch : IAggregateRoot
{
    private readonly List<RosterImportBatchRow> _rows = [];

    public int RosterImportBatchId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ImportedByEmail { get; private set; } = string.Empty;

    /// <summary>The company every row was imported into, when the whole file targeted one (the usual
    /// case - the SkillSets template carries a single organization name). Null when rows named their
    /// own companies individually.</summary>
    public int? CompanyId { get; private set; }

    public DateTimeOffset ImportedAt { get; private set; }

    public int TotalRows { get; private set; }

    public int CreatedCount { get; private set; }

    public int SkippedCount { get; private set; }

    public int FailedCount { get; private set; }

    public int EmployeesCreated { get; private set; }

    public int ManagersCreated { get; private set; }

    /// <summary>Null until the admin answers "Send Welcome Emails?". Doubles as the guard that stops
    /// the same batch being emailed twice.</summary>
    public DateTimeOffset? WelcomeEmailsSentAt { get; private set; }

    public int WelcomeEmailsSentCount { get; private set; }

    public IReadOnlyCollection<RosterImportBatchRow> Rows => _rows;

    private RosterImportBatch()
    {
    }

    public RosterImportBatch(string fileName, string importedByEmail, int? companyId)
    {
        FileName = fileName;
        ImportedByEmail = importedByEmail;
        CompanyId = companyId;
        ImportedAt = DateTimeOffset.UtcNow;
    }

    public void AddRow(RosterImportBatchRow row) => _rows.Add(row);

    public void SetTotals(int totalRows, int created, int skipped, int failed, int employeesCreated, int managersCreated)
    {
        TotalRows = totalRows;
        CreatedCount = created;
        SkippedCount = skipped;
        FailedCount = failed;
        EmployeesCreated = employeesCreated;
        ManagersCreated = managersCreated;
    }

    public void MarkWelcomeEmailsSent(int sentCount)
    {
        WelcomeEmailsSentAt = DateTimeOffset.UtcNow;
        WelcomeEmailsSentCount = sentCount;
    }
}
