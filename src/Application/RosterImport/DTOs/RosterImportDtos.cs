namespace SkillsetsBackend.Application.RosterImport.DTOs;

/// <summary>One row as the admin will see it in the results/preview table.</summary>
/// <param name="RowNumber">The line number in the source file, so it matches what Excel shows.</param>
/// <param name="Status">Created / Skipped / Failed - or, in a preview, WillCreate / WillSkip / Invalid.</param>
/// <param name="Reason">Plain-language explanation, e.g. "Email already exists".</param>
public record RosterRowResultDto(
    int RowNumber,
    string? Name,
    string? Email,
    string? CompanyName,
    string? EmployeeType,
    bool GiveManagerDashboard,
    string Status,
    string Reason);

/// <summary>
/// The counts shown above the results table. Deliberately more granular than created/skipped/failed
/// because the admin's real questions are "how many accounts do I now have?" and "what do I need to
/// go and fix?".
/// </summary>
public record RosterImportSummaryDto(
    int TotalRows,
    int SuccessfullyCreated,
    int Skipped,
    int Failed,
    int EmployeesCreated,
    int ManagersCreated,
    int AlreadyExistingUsers,
    int InvalidRecords,
    int MissingRequiredFields,
    int DuplicateRecords);

/// <summary>What the file looked like, echoed back so a mis-read file is obvious before anything is
/// written - which header row was found, which columns were recognised, and which company the rows
/// will land in.</summary>
public record RosterFileInfoDto(
    string FileName,
    int DetectedHeaderRow,
    IReadOnlyDictionary<string, string> MappedColumns,
    string? OrganizationNameInFile,
    int? ResolvedCompanyId,
    string? ResolvedCompanyName,
    IReadOnlyList<string> FileWarnings);

/// <summary>Dry run: exactly what an import would do, with nothing written.</summary>
public record RosterImportPreviewDto(
    RosterFileInfoDto File,
    RosterImportSummaryDto Summary,
    IReadOnlyList<RosterRowResultDto> Rows);

/// <summary>The outcome of a real import. BatchId is what the "Send welcome emails" step and the
/// results export are addressed to.</summary>
public record RosterImportResultDto(
    int BatchId,
    RosterFileInfoDto File,
    RosterImportSummaryDto Summary,
    IReadOnlyList<RosterRowResultDto> Rows,
    int EligibleForWelcomeEmail);

public record SendRosterWelcomeEmailsResultDto(int BatchId, int Sent, int Failed, string Message);

/// <summary>Answers "how many employees/managers were created manually vs by roster import" from the
/// stored CreationSource columns rather than by inference.</summary>
public record CreationSourceStatsDto(
    int EmployeesManual,
    int EmployeesRosterImport,
    int ManagersManual,
    int ManagersRosterImport,
    int EmployeesLegacy,
    int ManagersLegacy);
