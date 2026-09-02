namespace SkillsetsBackend.Application.RosterImport.Interfaces;

/// <summary>One roster line, straight out of the file with no interpretation beyond trimming.
/// RowNumber is the 1-based line number AS IT APPEARS IN EXCEL, so "row 47" points at the line the
/// admin can actually scroll to - not an index into the parsed subset.</summary>
public record RosterRawRow(
    int RowNumber,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? CompanyName,
    string? EmployeeType,
    string? Password,
    string? GiveManagerDashboard,
    string? UpdateAction);

/// <summary>
/// The result of reading a roster file.
/// </summary>
/// <param name="Rows">Every data row found, in file order.</param>
/// <param name="OrganizationName">
/// The company name taken from the SkillSets template's "Enter Your Organization's Name:" cell,
/// which sits ABOVE the header row and applies to the whole file. Null when the file has no such
/// cell - in that case the company comes from a per-row Company column, or from the company the
/// admin picks in the UI.
/// </param>
/// <param name="DetectedHeaderRowNumber">Which line the header was found on - surfaced in the
/// preview so a mis-detected header is obvious to the admin rather than silently wrong.</param>
/// <param name="MappedColumns">Header text -> the field it was matched to, shown in the preview so
/// the admin can confirm the file was understood the way they expect.</param>
public record RosterParseResult(
    IReadOnlyList<RosterRawRow> Rows,
    string? OrganizationName,
    int DetectedHeaderRowNumber,
    IReadOnlyDictionary<string, string> MappedColumns);

public interface IRosterImportFileParser
{
    /// <summary>Reads .xlsx, legacy .xls and .csv. Purely mechanical extraction - every business
    /// rule (defaults, duplicates, required fields) belongs in the handler.</summary>
    RosterParseResult Parse(Stream fileStream, string fileName);
}
