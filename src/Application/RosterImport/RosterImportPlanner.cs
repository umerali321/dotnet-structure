using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.RosterImport;

/// <summary>What the import will do with one row.</summary>
public enum RosterRowAction
{
    /// <summary>Create the account (and Manager access too, if the row asked for it).</summary>
    CreateUser,

    /// <summary>The person already exists but the row asks for the manager dashboard they don't yet
    /// have - grant it without creating a second account.</summary>
    GrantManagerToExistingUser,

    /// <summary>Valid, but nothing to do.</summary>
    Skip,

    /// <summary>Cannot be used at all.</summary>
    Fail,
}

public sealed record PlannedRosterRow(
    InterpretedRosterRow Row,
    RosterRowAction Action,
    string Reason,
    int CompanyId,
    string CompanyName,
    int? ExistingUserId);

public sealed record RosterImportPlan(
    RosterFileInfoDto FileInfo,
    IReadOnlyList<PlannedRosterRow> Rows,
    RosterImportSummaryDto Summary);

/// <summary>
/// Decides, for a whole file, exactly what the import will do - without writing anything. The
/// preview shows this plan and the import executes it, so the two can never drift apart.
/// </summary>
public class RosterImportPlanner
{
    private readonly IRosterImportRepository _repository;

    public RosterImportPlanner(IRosterImportRepository repository)
    {
        _repository = repository;
    }

    public async Task<RosterImportPlan> BuildAsync(
        RosterParseResult parsed,
        string fileName,
        int? requestedCompanyId,
        IReadOnlyCollection<int>? callerCompanyIds,
        CancellationToken cancellationToken)
    {
        var fileWarnings = new List<string>();
        var (defaultCompanyId, defaultCompanyName) =
            await ResolveTargetCompanyAsync(parsed, requestedCompanyId, callerCompanyIds, fileWarnings, cancellationToken);

        var interpreted = RosterRowInterpreter.Interpret(parsed.Rows);

        // One lookup for the whole file. Doing this per row would be a query per employee.
        var emails = interpreted
            .Where(r => r.Verdict == RosterRowVerdict.Valid && r.Email is not null)
            .Select(r => r.Email!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await _repository.FindExistingUsersByEmailAsync(emails, defaultCompanyId, cancellationToken);

        var planned = new List<PlannedRosterRow>(interpreted.Count);
        int created = 0, skipped = 0, failed = 0;
        int employees = 0, managers = 0, alreadyExisting = 0, invalid = 0, missingFields = 0, duplicates = 0;

        foreach (var row in interpreted)
        {
            switch (row.Verdict)
            {
                case RosterRowVerdict.MissingRequiredField:
                    missingFields++;
                    failed++;
                    planned.Add(new PlannedRosterRow(row, RosterRowAction.Fail, row.Reason, defaultCompanyId, defaultCompanyName, null));
                    continue;

                case RosterRowVerdict.Invalid:
                    invalid++;
                    failed++;
                    planned.Add(new PlannedRosterRow(row, RosterRowAction.Fail, row.Reason, defaultCompanyId, defaultCompanyName, null));
                    continue;

                case RosterRowVerdict.DuplicateInFile:
                    duplicates++;
                    skipped++;
                    planned.Add(new PlannedRosterRow(row, RosterRowAction.Skip, row.Reason, defaultCompanyId, defaultCompanyName, null));
                    continue;

                case RosterRowVerdict.MarkedForRemoval:
                    skipped++;
                    planned.Add(new PlannedRosterRow(row, RosterRowAction.Skip, row.Reason, defaultCompanyId, defaultCompanyName, null));
                    continue;
            }

            if (existing.TryGetValue(row.Email!, out var found))
            {
                alreadyExisting++;

                // An email that already exists SOMEWHERE must not become importable just because the
                // target company changed. Previously the only test was "do they lack the Manager role
                // at this company" - which is trivially true for a company they are not in at all, so
                // switching the company picker turned 63 already-existing people into "will create"
                // and would have granted them manager access at a company they have no connection to.
                //
                // Belonging to the target company is now the precondition for touching them.
                var belongsToTargetCompany = found.HasStudentRoleAtCompany || found.HasManagerRoleAtCompany;

                if (!belongsToTargetCompany)
                {
                    skipped++;
                    planned.Add(new PlannedRosterRow(row, RosterRowAction.Skip,
                        "Email already registered under a different company.",
                        defaultCompanyId, defaultCompanyName, found.UserId));
                    continue;
                }

                // Already at this company, and the row asks for the manager dashboard they don't yet
                // have. Granting it is not creating a duplicate user - it is the "Give Mgr Dashboard
                // = Yes" rule applied to somebody already on this company's roster.
                if (row.GiveManagerDashboard && !found.HasManagerRoleAtCompany)
                {
                    created++;
                    managers++;
                    planned.Add(new PlannedRosterRow(row, RosterRowAction.GrantManagerToExistingUser,
                        "Already existed - manager access added.", defaultCompanyId, defaultCompanyName, found.UserId));
                    continue;
                }

                skipped++;
                planned.Add(new PlannedRosterRow(row, RosterRowAction.Skip,
                    "Email already exists.", defaultCompanyId, defaultCompanyName, found.UserId));
                continue;
            }

            created++;
            employees++;
            if (row.GiveManagerDashboard)
            {
                managers++;
            }

            planned.Add(new PlannedRosterRow(row, RosterRowAction.CreateUser,
                row.GiveManagerDashboard ? "Employee + Manager" : "Employee",
                defaultCompanyId, defaultCompanyName, null));
        }

        var summary = new RosterImportSummaryDto(
            TotalRows: interpreted.Count,
            SuccessfullyCreated: created,
            Skipped: skipped,
            Failed: failed,
            EmployeesCreated: employees,
            ManagersCreated: managers,
            AlreadyExistingUsers: alreadyExisting,
            InvalidRecords: invalid,
            MissingRequiredFields: missingFields,
            DuplicateRecords: duplicates);

        var fileInfo = new RosterFileInfoDto(
            fileName,
            parsed.DetectedHeaderRowNumber,
            parsed.MappedColumns,
            parsed.OrganizationName,
            defaultCompanyId,
            defaultCompanyName,
            fileWarnings);

        return new RosterImportPlan(fileInfo, planned, summary);
    }

    /// <summary>
    /// Which company these people belong to, in order of precedence:
    ///   1. The company the admin explicitly picked - an unambiguous instruction.
    ///   2. The company named in the file, IF the caller may use it.
    ///   3. The caller's own company, when they only have one.
    ///
    /// Point 3 is what stops a Company Admin having to think about this at all: they are signed in
    /// to exactly one company, so a file that names nothing readable - or names someone else's
    /// company - imports into theirs with a warning saying so. Only a platform admin, who belongs to
    /// no company, is ever asked to choose.
    ///
    /// Every refusal here names BOTH companies. The previous version threw a bare "You do not have
    /// access to that company", which the UI turned into "You do not have permission to import an
    /// employee roster" - telling a Company Admin they lacked a permission they actually had, when
    /// the real problem was that the file was for a different company.
    /// </summary>
    private async Task<(int CompanyId, string CompanyName)> ResolveTargetCompanyAsync(
        RosterParseResult parsed,
        int? requestedCompanyId,
        IReadOnlyCollection<int>? callerCompanyIds,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // 1. Explicit choice.
        if (requestedCompanyId is not null)
        {
            var picked = await _repository.FindCompanyByIdAsync(requestedCompanyId.Value, cancellationToken)
                ?? throw Invalid($"Company {requestedCompanyId} was not found.");

            if (callerCompanyIds is not null && !callerCompanyIds.Contains(picked.CompanyId))
            {
                throw Invalid($"You are not signed in to '{picked.CompanyName}', so you cannot import "
                              + "people into it.");
            }

            if (parsed.OrganizationName is not null
                && !string.Equals(parsed.OrganizationName, picked.CompanyName, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"The file names '{parsed.OrganizationName}', but you selected "
                             + $"'{picked.CompanyName}'. The selected company will be used.");
            }

            return picked;
        }

        var ownCompany = await ResolveCallersOwnCompanyAsync(callerCompanyIds, cancellationToken);
        var nameFromFile = parsed.OrganizationName ?? SingleCompanyNameFromRows(parsed);

        // 2. The file names a company.
        if (nameFromFile is not null)
        {
            var fromFile = await _repository.FindCompanyByNameAsync(nameFromFile, cancellationToken);

            if (fromFile is not null && (callerCompanyIds is null || callerCompanyIds.Contains(fromFile.Value.CompanyId)))
            {
                return fromFile.Value;
            }

            // The file names a company this caller cannot use (or one that does not exist here).
            // For someone signed in to a single company that is recoverable - use theirs and say so
            // plainly, naming both, instead of refusing with a permissions-sounding error.
            if (ownCompany is not null)
            {
                warnings.Add(fromFile is null
                    ? $"The file names '{nameFromFile}', which is not a company in SkillSets. "
                      + $"Importing into your own company, '{ownCompany.Value.CompanyName}', instead."
                    : $"The file is for '{fromFile.Value.CompanyName}', but you are signed in to "
                      + $"'{ownCompany.Value.CompanyName}'. Importing into '{ownCompany.Value.CompanyName}'.");
                return ownCompany.Value;
            }

            throw Invalid(fromFile is null
                ? $"The file names '{nameFromFile}', which is not a company in SkillSets. "
                  + "Create it first, or pick an existing company above."
                : $"The file is for '{fromFile.Value.CompanyName}', which you are not signed in to. "
                  + "Pick a company you have access to above.");
        }

        // 3. The file says nothing usable.
        if (ownCompany is not null)
        {
            warnings.Add($"The file does not name a company, so your own company "
                         + $"('{ownCompany.Value.CompanyName}') is being used.");
            return ownCompany.Value;
        }

        throw Invalid("This file does not say which company these people belong to. "
                      + "Pick a company above before importing.");
    }

    /// <summary>The caller's company when they have exactly one - the case where "just use mine" is
    /// unambiguous. Null for a platform admin (no company at all) and for anyone spanning several,
    /// since guessing between them would be worse than asking.</summary>
    private async Task<(int CompanyId, string CompanyName)?> ResolveCallersOwnCompanyAsync(
        IReadOnlyCollection<int>? callerCompanyIds,
        CancellationToken cancellationToken)
    {
        if (callerCompanyIds is null || callerCompanyIds.Count != 1)
        {
            return null;
        }

        return await _repository.FindCompanyByIdAsync(callerCompanyIds.First(), cancellationToken);
    }

    /// <summary>A per-row Company column is only usable when the whole file agrees - a file spanning
    /// several companies is refused rather than silently importing everyone into the first one.</summary>
    private static string? SingleCompanyNameFromRows(RosterParseResult parsed)
    {
        var names = parsed.Rows
            .Select(r => r.CompanyName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return names.Count switch
        {
            1 => names[0],
            > 1 => throw Invalid("The file's Company column names more than one company "
                                 + $"({string.Join(", ", names.Take(5))}). Import one company at a time, "
                                 + "or select a company to import everyone into."),
            _ => null,
        };
    }

    private static AppValidationException Invalid(string message) =>
        new([new ValidationFailure("File", message)]);
}
