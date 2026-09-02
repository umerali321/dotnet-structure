namespace SkillsetsBackend.Domain.RosterImport;

/// <summary>The per-row outcome of a roster import - one row of the results table the admin sees,
/// kept so the results survive a refresh and can be exported afterwards.</summary>
public class RosterImportBatchRow
{
    public int RosterImportBatchRowId { get; private set; }

    public int RosterImportBatchId { get; private set; }

    /// <summary>The row number as it appears in the source file, so "row 47 failed" points at the
    /// line the admin can actually see in Excel - not an index into the parsed subset.</summary>
    public int RowNumber { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Email { get; private set; }

    public string? CompanyName { get; private set; }

    public string? EmployeeType { get; private set; }

    public bool GiveManagerDashboard { get; private set; }

    /// <summary>See <see cref="RosterImportRowStatus"/>.</summary>
    public string Status { get; private set; } = RosterImportRowStatus.Failed;

    /// <summary>Why, in words the admin can act on ("Email already exists", "Email is required").</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Set only for rows that actually created an account - this is what the welcome-email
    /// step iterates over.</summary>
    public int? UserId { get; private set; }

    public bool EmployeeCreated { get; private set; }

    public bool ManagerCreated { get; private set; }

    private RosterImportBatchRow()
    {
    }

    public RosterImportBatchRow(
        int rowNumber,
        string? firstName,
        string? lastName,
        string? email,
        string? companyName,
        string? employeeType,
        bool giveManagerDashboard,
        string status,
        string reason,
        int? userId = null,
        bool employeeCreated = false,
        bool managerCreated = false)
    {
        RowNumber = rowNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        CompanyName = companyName;
        EmployeeType = employeeType;
        GiveManagerDashboard = giveManagerDashboard;
        Status = status;
        Reason = reason;
        UserId = userId;
        EmployeeCreated = employeeCreated;
        ManagerCreated = managerCreated;
    }
}

/// <summary>The four outcomes a roster row can have. "Skipped" means the row was valid but nothing
/// needed doing (the person already exists); "Failed" means the row could not be used at all.</summary>
public static class RosterImportRowStatus
{
    public const string Created = "Created";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";

    public const int MaxLength = 20;
}
