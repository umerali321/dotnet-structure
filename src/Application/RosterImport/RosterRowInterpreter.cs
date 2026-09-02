using System.Net.Mail;
using System.Security.Cryptography;
using SkillsetsBackend.Application.RosterImport.Interfaces;

namespace SkillsetsBackend.Application.RosterImport;

/// <summary>Why a row cannot be used, or that it can. Kept as a small closed set so the preview and
/// the real import can never disagree about a row - the preview's promise is only worth something
/// if it applies the exact same rules.</summary>
public enum RosterRowVerdict
{
    Valid,
    MissingRequiredField,
    Invalid,
    DuplicateInFile,
    MarkedForRemoval,
}

/// <summary>A roster row after cleaning, defaulting and validation - everything the import needs and
/// nothing about the file it came from.</summary>
public sealed class InterpretedRosterRow
{
    public required int RowNumber { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? CompanyNameInRow { get; init; }

    /// <summary>Always populated - defaults to NON-IT when the file leaves it blank.</summary>
    public required string EmployeeType { get; init; }

    /// <summary>The password to assign: the file's own value when it supplied one, otherwise a
    /// freshly generated 4-digit code.</summary>
    public required string Password { get; init; }

    public required bool GiveManagerDashboard { get; init; }

    public required RosterRowVerdict Verdict { get; init; }

    /// <summary>Empty when Verdict is Valid.</summary>
    public string Reason { get; init; } = string.Empty;

    public string DisplayName =>
        string.Join(' ', new[] { FirstName, LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
}

/// <summary>
/// Turns raw roster lines into validated rows, applying every business rule that does not need the
/// database. Shared by the preview and the import for exactly that reason.
/// </summary>
public static class RosterRowInterpreter
{
    /// <summary>NON-IT when the file says nothing - the documented default.</summary>
    public const string DefaultEmployeeType = "NON-IT";

    public const string ItType = "IT";

    public static IReadOnlyList<InterpretedRosterRow> Interpret(IReadOnlyList<RosterRawRow> rows)
    {
        var result = new List<InterpretedRosterRow>(rows.Count);
        var firstSeenAtRow = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var firstName = Trim(row.FirstName);
            var lastName = Trim(row.LastName);
            var email = Trim(row.Email);
            var employeeType = NormalizeEmployeeType(row.EmployeeType);
            var giveManager = ParseYesNo(row.GiveManagerDashboard);
            var password = ResolvePassword(row.Password);

            InterpretedRosterRow Build(RosterRowVerdict verdict, string reason) => new()
            {
                RowNumber = row.RowNumber,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = Trim(row.Phone),
                CompanyNameInRow = Trim(row.CompanyName),
                EmployeeType = employeeType,
                Password = password,
                GiveManagerDashboard = giveManager,
                Verdict = verdict,
                Reason = reason,
            };

            // The SkillSets template's "UPDATE: Add or Remove" column. A Remove line is an
            // instruction to take someone OFF the roster - importing it would create the very
            // account the file is asking to remove.
            var action = Trim(row.UpdateAction);
            if (action is not null && action.StartsWith("remove", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Build(RosterRowVerdict.MarkedForRemoval, "Row is marked 'Remove' in the Update column."));
                continue;
            }

            var missing = new List<string>();
            if (firstName is null) missing.Add("First Name");
            if (lastName is null) missing.Add("Last Name");
            if (email is null) missing.Add("Email");

            if (missing.Count > 0)
            {
                result.Add(Build(RosterRowVerdict.MissingRequiredField,
                    missing.Count == 1 ? $"{missing[0]} is required." : $"Required field(s) missing: {string.Join(", ", missing)}."));
                continue;
            }

            if (!MailAddress.TryCreate(email, out _))
            {
                result.Add(Build(RosterRowVerdict.Invalid, $"'{email}' is not a valid email address."));
                continue;
            }

            if (firstSeenAtRow.TryGetValue(email!, out var firstRow))
            {
                result.Add(Build(RosterRowVerdict.DuplicateInFile, $"Duplicate of row {firstRow} in this file."));
                continue;
            }

            firstSeenAtRow[email!] = row.RowNumber;
            result.Add(Build(RosterRowVerdict.Valid, string.Empty));
        }

        return result;
    }

    /// <summary>
    /// Real files use IT / NON / NON_IT / NON-IT interchangeably, and often leave the column blank
    /// or absent altogether. Anything that isn't recognisably IT becomes the NON-IT default rather
    /// than failing the row - this field only drives report grouping, so a bad value is not worth
    /// refusing an account over.
    /// </summary>
    public static string NormalizeEmployeeType(string? raw)
    {
        var value = Trim(raw);
        if (value is null)
        {
            return DefaultEmployeeType;
        }

        var compact = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return compact == "IT" ? ItType : DefaultEmployeeType;
    }

    /// <summary>Yes/Y/True/1 mean yes; everything else (including blank) means no. "Give Mgr
    /// Dashboard" defaulting to yes on an unreadable value would silently hand out manager
    /// access.</summary>
    public static bool ParseYesNo(string? raw)
    {
        var value = Trim(raw);
        if (value is null)
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() is "yes" or "y" or "true" or "1";
    }

    /// <summary>The file's password when it supplied one - taken exactly as written, since these are
    /// credentials people may already have been given. Otherwise a 4-digit code that never starts
    /// with 0, so it survives being retyped (or re-opened in Excel) as a number.</summary>
    public static string ResolvePassword(string? rawPassword)
    {
        var provided = Trim(rawPassword);
        return provided ?? GenerateFourDigitPassword();
    }

    public static string GenerateFourDigitPassword() =>
        RandomNumberGenerator.GetInt32(1000, 10000).ToString();

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
