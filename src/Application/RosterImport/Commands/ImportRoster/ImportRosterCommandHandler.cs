using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.Commands.PreviewRosterImport;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using SkillsetsBackend.Domain.RosterImport;

namespace SkillsetsBackend.Application.RosterImport.Commands.ImportRoster;

public record ImportRosterCommand(Stream FileStream, string FileName, int? CompanyId);

/// <summary>
/// Step 4 of the wizard: actually creates the accounts.
///
/// Deliberately NO welcome emails here. The admin is asked "Send welcome emails?" only after seeing
/// the results, and answers it as a separate confirmed step - see
/// SendRosterWelcomeEmailsCommandHandler. That is also why the batch and its rows are persisted:
/// the set of accounts this run created has to still be knowable afterwards.
/// </summary>
public class ImportRosterCommandHandler
{
    private readonly IRosterImportFileParser _parser;
    private readonly RosterImportPlanner _planner;
    private readonly IRosterImportRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IUserDirectory _userDirectory;
    private readonly ILogger<ImportRosterCommandHandler> _logger;

    public ImportRosterCommandHandler(
        IRosterImportFileParser parser,
        RosterImportPlanner planner,
        IRosterImportRepository repository,
        IPermissionService permissionService,
        IUserDirectory userDirectory,
        ILogger<ImportRosterCommandHandler> logger)
    {
        _parser = parser;
        _planner = planner;
        _repository = repository;
        _permissionService = permissionService;
        _userDirectory = userDirectory;
        _logger = logger;
    }

    public async Task<RosterImportResultDto> Handle(
        ImportRosterCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var callerCompanyIds = await RosterImportAuthorization.AuthorizeAsync(
            caller, _permissionService, _userDirectory, cancellationToken);

        var parsed = _parser.Parse(command.FileStream, command.FileName);
        var plan = await _planner.BuildAsync(
            parsed, command.FileName, command.CompanyId, callerCompanyIds, cancellationToken);

        var batch = new RosterImportBatch(command.FileName, caller.Email, plan.FileInfo.ResolvedCompanyId);

        int created = 0, skipped = 0, failed = 0, employees = 0, managers = 0;

        foreach (var planned in plan.Rows)
        {
            var row = planned.Row;

            if (planned.Action == RosterRowAction.Fail)
            {
                failed++;
                batch.AddRow(Row(planned, RosterImportRowStatus.Failed, planned.Reason));
                continue;
            }

            if (planned.Action == RosterRowAction.Skip)
            {
                skipped++;
                batch.AddRow(Row(planned, RosterImportRowStatus.Skipped, planned.Reason));
                continue;
            }

            try
            {
                if (planned.Action == RosterRowAction.GrantManagerToExistingUser)
                {
                    await _repository.GrantManagerRoleAsync(planned.ExistingUserId!.Value, planned.CompanyId, cancellationToken);
                    created++;
                    managers++;
                    batch.AddRow(Row(planned, RosterImportRowStatus.Created, "Already existed - manager access added.",
                        userId: planned.ExistingUserId, employeeCreated: false, managerCreated: true));
                    continue;
                }

                var userId = await _repository.CreateRosterUserAsync(
                    row.Email!, row.Phone, row.FirstName!, row.LastName!, row.Password,
                    row.EmployeeType, planned.CompanyId, row.GiveManagerDashboard, caller.Email, cancellationToken);

                created++;
                employees++;
                if (row.GiveManagerDashboard)
                {
                    managers++;
                }

                batch.AddRow(Row(planned, RosterImportRowStatus.Created,
                    row.GiveManagerDashboard ? "Employee + Manager" : "Employee",
                    userId: userId, employeeCreated: true, managerCreated: row.GiveManagerDashboard));
            }
            catch (Exception ex)
            {
                // One bad row must never abort the file. CreateRosterUserAsync is transactional per
                // person, so nothing partial is left behind by the row that threw.
                failed++;
                _logger.LogWarning(ex, "Roster import: row {RowNumber} ({Email}) failed.", row.RowNumber, row.Email);
                batch.AddRow(Row(planned, RosterImportRowStatus.Failed, Describe(ex)));
            }
        }

        batch.SetTotals(plan.Summary.TotalRows, created, skipped, failed, employees, managers);
        var batchId = await _repository.SaveBatchAsync(batch, cancellationToken);

        var rows = batch.Rows
            .Select(r => new RosterRowResultDto(
                r.RowNumber, JoinName(r.FirstName, r.LastName), r.Email, r.CompanyName,
                r.EmployeeType, r.GiveManagerDashboard, r.Status, r.Reason))
            .OrderBy(r => r.RowNumber)
            .ToList();

        var summary = plan.Summary with
        {
            SuccessfullyCreated = created,
            Skipped = skipped,
            Failed = failed,
            EmployeesCreated = employees,
            ManagersCreated = managers,
        };

        return new RosterImportResultDto(
            batchId,
            plan.FileInfo,
            summary,
            rows,
            EligibleForWelcomeEmail: batch.Rows.Count(r => r.EmployeeCreated));
    }

    private static RosterImportBatchRow Row(
        PlannedRosterRow planned,
        string status,
        string reason,
        int? userId = null,
        bool employeeCreated = false,
        bool managerCreated = false) =>
        new(planned.Row.RowNumber, planned.Row.FirstName, planned.Row.LastName, planned.Row.Email,
            planned.CompanyName, planned.Row.EmployeeType, planned.Row.GiveManagerDashboard,
            status, Truncate(reason), userId, employeeCreated, managerCreated);

    private static string JoinName(string? first, string? last) =>
        string.Join(' ', new[] { first, last }.Where(p => !string.IsNullOrWhiteSpace(p)));

    /// <summary>Reason is a 500-character column; a stack-trace-length message would otherwise fail
    /// the whole save after every row had already been created.</summary>
    private static string Truncate(string value) =>
        value.Length <= 500 ? value : value[..497] + "...";

    private static string Describe(Exception ex) => ex switch
    {
        Common.Exceptions.ValidationException validation =>
            string.Join(" ", validation.Errors.SelectMany(e => e.Value)),
        _ => ex.Message,
    };
}
