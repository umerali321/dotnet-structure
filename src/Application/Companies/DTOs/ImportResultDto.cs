namespace SkillsetsBackend.Application.Companies.DTOs;

public record ImportSummaryDto(
    int TotalRows,
    int NewCompaniesCreated,
    int ExistingCompaniesUpdated,
    int CompanyAdminsCreated,
    int CompanyAdminsUpdated,
    int SkippedAlreadyComplete,
    int ValidationErrors,
    int ImportFailed);

/// <summary>Status: "Created" | "Updated" | "NoChangesRequired" | "ValidationError" | "ImportFailed".</summary>
public record ImportRowResultDto(
    int RowNumber,
    string? CompanyCode,
    string? CompanyName,
    string Status,
    string Message,
    IReadOnlyList<string> Warnings);

public record ImportResultDto(ImportSummaryDto Summary, IReadOnlyList<ImportRowResultDto> Rows);
