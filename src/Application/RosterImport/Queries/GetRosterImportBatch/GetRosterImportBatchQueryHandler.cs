using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.Commands.PreviewRosterImport;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RosterImport.Queries.GetRosterImportBatch;

/// <summary>Re-reads a finished import's results - what the results screen redraws from after a
/// refresh, and what the "download results" export renders.</summary>
public class GetRosterImportBatchQueryHandler
{
    private readonly IRosterImportRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IUserDirectory _userDirectory;

    public GetRosterImportBatchQueryHandler(
        IRosterImportRepository repository,
        IPermissionService permissionService,
        IUserDirectory userDirectory)
    {
        _repository = repository;
        _permissionService = permissionService;
        _userDirectory = userDirectory;
    }

    public async Task<RosterImportResultDto> Handle(int batchId, CallerContext caller, CancellationToken cancellationToken)
    {
        await RosterImportAuthorization.AuthorizeAsync(caller, _permissionService, _userDirectory, cancellationToken);

        var batch = await _repository.GetBatchAsync(batchId, cancellationToken)
            ?? throw new NotFoundException("Roster import batch", batchId);

        var rows = batch.Rows
            .Select(r => new RosterRowResultDto(
                r.RowNumber,
                string.Join(' ', new[] { r.FirstName, r.LastName }.Where(p => !string.IsNullOrWhiteSpace(p))),
                r.Email, r.CompanyName, r.EmployeeType, r.GiveManagerDashboard, r.Status, r.Reason))
            .OrderBy(r => r.RowNumber)
            .ToList();

        var summary = new RosterImportSummaryDto(
            batch.TotalRows,
            batch.CreatedCount,
            batch.SkippedCount,
            batch.FailedCount,
            batch.EmployeesCreated,
            batch.ManagersCreated,
            AlreadyExistingUsers: rows.Count(r => r.Reason.Contains("already exists", StringComparison.OrdinalIgnoreCase)),
            InvalidRecords: rows.Count(r => r.Reason.Contains("not a valid", StringComparison.OrdinalIgnoreCase)),
            MissingRequiredFields: rows.Count(r => r.Reason.Contains("required", StringComparison.OrdinalIgnoreCase)),
            DuplicateRecords: rows.Count(r => r.Reason.StartsWith("Duplicate", StringComparison.OrdinalIgnoreCase)));

        var fileInfo = new RosterFileInfoDto(
            batch.FileName,
            DetectedHeaderRow: 0,
            MappedColumns: new Dictionary<string, string>(),
            OrganizationNameInFile: null,
            ResolvedCompanyId: batch.CompanyId,
            ResolvedCompanyName: rows.FirstOrDefault()?.CompanyName,
            FileWarnings: []);

        return new RosterImportResultDto(
            batch.RosterImportBatchId,
            fileInfo,
            summary,
            rows,
            // Zero once the emails have been dealt with, so the prompt isn't offered twice.
            EligibleForWelcomeEmail: batch.WelcomeEmailsSentAt is null
                ? batch.Rows.Count(r => r.EmployeeCreated)
                : 0);
    }
}
