using System.Data;
using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Application.LearningTranscript.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.LearningTranscript;

/// <summary>
/// Parses a Skillport "Asset Activity by User" .xlsx export (the shape skillport-scraper produces)
/// and loads it via the dbo.sp_ImportLearningTranscriptBatch stored procedure, passing every row as
/// one table-valued parameter instead of one round-trip per row.
///
/// The source report groups activity rows under a per-person header row that contains ONLY that
/// person's Skillport username (e.g. "adachhn") with every other cell blank - confirmed from the
/// user's own sample export. This class "carries down" that username onto every subsequent data
/// row until the next such header row appears, since the raw file has no per-row username column
/// of its own. A header row is detected as: the first cell has text, and every other mapped column
/// on that row is blank - a real data row always has values in far more than just column 1.
/// </summary>
public class LearningTranscriptImportService : ILearningTranscriptImportService
{
    private readonly ApplicationDbContext _dbContext;

    public LearningTranscriptImportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Expected header text -> DataTable column name. Matched case-insensitively/trimmed so minor
    // capitalization differences in a future export don't break parsing. This array's own order
    // doesn't matter - every lookup against it is by name, not position.
    private static readonly (string Header, string Column)[] ColumnMap =
    [
        ("User ID", "SkillportUserIdText"),
        ("First Name", "FirstName"),
        ("Last Name", "LastName"),
        ("Display First Name", "DisplayFirstName"),
        ("Display Last Name", "DisplayLastName"),
        ("Location", "Location"),
        ("User Status", "UserStatus"),
        ("Group Name", "GroupName"),
        ("Group Org Code", "GroupOrgCode"),
        ("Group Path", "GroupPath"),
        ("Asset Title", "AssetTitle"),
        ("Asset ID", "AssetId"),
        ("Asset Type", "AssetType"),
        ("Asset Sub-type", "AssetSubType"),
        ("Times Restarted", "TimesRestarted"),
        ("Absolute First Access Date", "AbsoluteFirstAccessDate"),
        ("Absolute Last Access Date", "AbsoluteLastAccessDate"),
        ("Absolute Times Accessed", "AbsoluteTimesAccessed"),
        ("Absolute High Score", "AbsoluteHighScore"),
        ("Absolute Last Score", "AbsoluteLastScore"),
        ("Absolute Actual Duration", "AbsoluteActualDurationMinutes"),
        ("First Access Date", "FirstAccessDate"),
        ("Last Access Date", "LastAccessDate"),
        ("Times Accessed", "TimesAccessed"),
        ("Times Downloaded", "TimesDownloaded"),
        ("Download Date", "DownloadDate"),
        ("HTML Page Reads", "HtmlPageReads"),
        ("Enrollment Date", "EnrollmentDate"),
        ("Completion Date", "CompletionDate"),
        ("Completion Status", "CompletionStatus"),
        ("Pre-test", "PreTestScore"),
        ("Max Test Attempts", "MaxTestAttempts"),
        ("Actual Test Attempts", "ActualTestAttempts"),
        ("High Score", "HighScore"),
        ("Current Score", "CurrentScore"),
        ("Expected Duration", "ExpectedDurationMinutes"),
        ("Actual Duration", "ActualDurationMinutes"),
        ("Last Skillport Login Date", "LastSkillportLoginDate"),
        ("Skillport Registration Date", "SkillportRegistrationDate"),
        ("Approval Manager ID", "ApprovalManagerId"),
        ("Approval Manager First Name", "ApprovalManagerFirstName"),
        ("Approval Manager Last Name", "ApprovalManagerLastName"),
        ("Email Address", "EmailAddress"),
    ];

    private static readonly string[] DurationColumns = ["AbsoluteActualDurationMinutes", "ExpectedDurationMinutes", "ActualDurationMinutes"];
    private static readonly string[] DateColumns =
    [
        "AbsoluteFirstAccessDate", "AbsoluteLastAccessDate", "FirstAccessDate", "LastAccessDate", "DownloadDate",
        "EnrollmentDate", "CompletionDate", "LastSkillportLoginDate", "SkillportRegistrationDate",
    ];
    private static readonly string[] DecimalColumns = ["AbsoluteHighScore", "AbsoluteLastScore", "PreTestScore", "HighScore", "CurrentScore"];
    private static readonly string[] IntColumns = ["TimesRestarted", "AbsoluteTimesAccessed", "TimesAccessed", "TimesDownloaded", "HtmlPageReads", "MaxTestAttempts", "ActualTestAttempts"];

    // Exact column order AND type the dbo.LearningTranscriptRowType TVP was declared with (see the
    // AddLearningTranscriptTables migration). SqlClient matches a table-valued parameter's
    // DataTable columns to the SQL type POSITIONALLY, not by name, AND needs each DataColumn's CLR
    // type to actually match what the corresponding TVP column expects (INT/DECIMAL/DATE, not a
    // plain string for all of them) - both have to be right or rows silently land in the wrong
    // column or get rejected. DateOnly is stored as DateTime here since System.Data.DataColumn has
    // no native DateOnly support; SQL Server's DATE type accepts a DateTime's date part as-is.
    private static readonly (string Column, Type ClrType)[] TvpColumns =
    [
        ("SkillportUsername", typeof(string)),
        ("SkillportUserIdText", typeof(string)),
        ("FirstName", typeof(string)),
        ("LastName", typeof(string)),
        ("DisplayFirstName", typeof(string)),
        ("DisplayLastName", typeof(string)),
        ("Location", typeof(string)),
        ("UserStatus", typeof(string)),
        ("GroupName", typeof(string)),
        ("GroupOrgCode", typeof(string)),
        ("GroupPath", typeof(string)),
        ("AssetId", typeof(string)),
        ("AssetTitle", typeof(string)),
        ("AssetType", typeof(string)),
        ("AssetSubType", typeof(string)),
        ("TimesRestarted", typeof(int)),
        ("AbsoluteFirstAccessDate", typeof(DateTime)),
        ("AbsoluteLastAccessDate", typeof(DateTime)),
        ("AbsoluteTimesAccessed", typeof(int)),
        ("AbsoluteHighScore", typeof(decimal)),
        ("AbsoluteLastScore", typeof(decimal)),
        ("AbsoluteActualDurationMinutes", typeof(int)),
        ("FirstAccessDate", typeof(DateTime)),
        ("LastAccessDate", typeof(DateTime)),
        ("TimesAccessed", typeof(int)),
        ("TimesDownloaded", typeof(int)),
        ("DownloadDate", typeof(DateTime)),
        ("HtmlPageReads", typeof(int)),
        ("EnrollmentDate", typeof(DateTime)),
        ("CompletionDate", typeof(DateTime)),
        ("CompletionStatus", typeof(string)),
        ("PreTestScore", typeof(decimal)),
        ("MaxTestAttempts", typeof(int)),
        ("ActualTestAttempts", typeof(int)),
        ("HighScore", typeof(decimal)),
        ("CurrentScore", typeof(decimal)),
        ("ExpectedDurationMinutes", typeof(int)),
        ("ActualDurationMinutes", typeof(int)),
        ("LastSkillportLoginDate", typeof(DateTime)),
        ("SkillportRegistrationDate", typeof(DateTime)),
        ("ApprovalManagerId", typeof(string)),
        ("ApprovalManagerFirstName", typeof(string)),
        ("ApprovalManagerLastName", typeof(string)),
        ("EmailAddress", typeof(string)),
    ];

    // A fallback column layout that doesn't depend on any header row at all: one field per column,
    // in exactly ColumnMap's own declared order. Confirmed live: a real file can contain whole
    // stretches of data rows in this single-column-per-field layout with no header row of their own
    // to re-sync against (see ParseWorkbookIntoTable's layout-selection comment) - unlike the other
    // two candidates there, which both derive from whatever header row was actually found.
    private static readonly List<(string Column, int ColumnIndex)> SequentialColumns =
        ColumnMap.Select((m, index) => (m.Column, ColumnIndex: index + 1)).ToList();

    public async Task<LearningTranscriptImportResultDto> ImportAsync(
        Stream fileContent,
        string sourceFileName,
        string importedBy,
        CancellationToken cancellationToken = default)
    {
        var table = ParseWorkbookIntoTable(fileContent);

        var connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_ImportLearningTranscriptBatch";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 120; // a full multi-thousand-row import batch can take longer than the 30s default

            var rowsParam = command.Parameters.AddWithValue("@Rows", table);
            rowsParam.SqlDbType = SqlDbType.Structured;
            rowsParam.TypeName = "dbo.LearningTranscriptRowType";

            command.Parameters.AddWithValue("@SourceFileName", sourceFileName);
            command.Parameters.AddWithValue("@ImportedBy", importedBy);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("sp_ImportLearningTranscriptBatch returned no summary row.");
            }

            var unmatchedGroupOrgCodesOrdinal = reader.GetOrdinal("UnmatchedGroupOrgCodes");
            return new LearningTranscriptImportResultDto(
                reader.GetInt32(reader.GetOrdinal("ImportBatchId")),
                reader.GetInt32(reader.GetOrdinal("TotalRows")),
                reader.GetInt32(reader.GetOrdinal("MatchedCount")),
                reader.GetInt32(reader.GetOrdinal("UnmatchedCount")),
                reader.IsDBNull(unmatchedGroupOrgCodesOrdinal) ? null : reader.GetString(unmatchedGroupOrgCodesOrdinal));
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static DataTable ParseWorkbookIntoTable(Stream fileContent)
    {
        var table = new DataTable();
        foreach (var (column, clrType) in TvpColumns)
        {
            table.Columns.Add(column, clrType);
        }

        using var workbook = new XLWorkbook(fileContent);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RangeUsed()?.RowsUsed().ToList() ?? [];
        if (rows.Count == 0)
        {
            return table;
        }

        var knownHeaderTexts = new HashSet<string>(ColumnMap.Select(m => m.Header), StringComparer.OrdinalIgnoreCase);

        // A single file can be several report exports pasted/appended together (confirmed live -
        // the same person and course reappeared at entirely different column numbers further down
        // one real file), each with its own header row and its own column layout. Re-detecting the
        // header - not just reading it once from row 1 - means every section parses against its
        // OWN actual column positions instead of whichever layout happened to be first.
        static bool LooksLikeHeaderRow(IXLRangeRow row, HashSet<string> knownHeaders)
        {
            var matches = 0;
            foreach (var cell in row.CellsUsed())
            {
                if (knownHeaders.Contains(cell.GetString().Trim())) matches++;
                if (matches >= 10) return true;
            }

            return false;
        }

        var resolvedColumns = new List<(string Column, int ColumnIndex)>();
        var currentSkillportUsername = string.Empty;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            if (LooksLikeHeaderRow(row, knownHeaderTexts))
            {
                var columnIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var cell in row.CellsUsed())
                {
                    var text = cell.GetString().Trim();
                    if (text.Length > 0)
                    {
                        columnIndexByName[text] = cell.WorksheetColumn().ColumnNumber();
                    }
                }

                resolvedColumns = ColumnMap
                    .Select(m => (m.Column, ColumnIndex: columnIndexByName.GetValueOrDefault(m.Header, -1)))
                    .Where(m => m.ColumnIndex > 0)
                    .ToList();
                continue;
            }

            // Confirmed live: the same file can encode data rows in more than one column layout with
            // no textual marker to re-sync on (unlike a whole new header row, which the loop above
            // already handles) - concretely, one real file mixed three layouts: (a) the header's own
            // layout as detected, (b) that same layout with a genuine "Group Path" value (e.g.
            // "/skillsets") silently inserted, shifting everything from Asset Title onward 2 columns
            // right, and (c) an entirely different single-column-per-field layout (SequentialColumns)
            // matching no header row in the file at all. No real course title ever starts with "/" or
            // is a bare number, and no real Asset ID is ever a bare number either (Skillsoft's own
            // codes always mix letters/underscores, e.g. "bs_ald17_a03_enus") - that signature picks
            // whichever candidate layout actually produces plausible values for THIS row, rather than
            // assuming one fixed layout for the whole file.
            var headerTitleColumnIndex = resolvedColumns.FirstOrDefault(c => c.Column == "AssetTitle").ColumnIndex;

            var candidateLayouts = new List<List<(string Column, int ColumnIndex)>>();
            if (resolvedColumns.Count > 0)
            {
                candidateLayouts.Add(resolvedColumns);
                if (headerTitleColumnIndex > 0)
                {
                    candidateLayouts.Add(resolvedColumns
                        .Select(c => (c.Column, ColumnIndex: c.ColumnIndex >= headerTitleColumnIndex ? c.ColumnIndex + 2 : c.ColumnIndex))
                        .ToList());
                }
            }

            candidateLayouts.Add(SequentialColumns);

            var activeColumns = candidateLayouts[0];
            foreach (var candidate in candidateLayouts)
            {
                var titleIndex = candidate.FirstOrDefault(c => c.Column == "AssetTitle").ColumnIndex;
                var idIndex = candidate.FirstOrDefault(c => c.Column == "AssetId").ColumnIndex;
                if (titleIndex <= 0 || idIndex <= 0)
                {
                    continue;
                }

                if (LooksLikeRealAssetTitle(row.Cell(titleIndex).GetString().Trim())
                    && LooksLikeRealAssetId(row.Cell(idIndex).GetString().Trim()))
                {
                    activeColumns = candidate;
                    break;
                }
            }

            var cells = activeColumns.ToDictionary(c => c.Column, c => row.Cell(c.ColumnIndex));

            var firstCell = row.FirstCellUsed()?.GetString().Trim() ?? string.Empty;
            // "User ID" (-> SkillportUserIdText) shares its column with the group-header row's own
            // username text - exclude it here or a group-header row's single populated cell would
            // count as "another value" against itself and never be detected as a header row.
            var hasAnyOtherValue = cells.Any(kv => kv.Key != "SkillportUserIdText" && !kv.Value.IsEmpty());

            if (firstCell.Length > 0 && !hasAnyOtherValue)
            {
                // A pure group-header row: only the person's Skillport username is present.
                currentSkillportUsername = firstCell;
                continue;
            }

            // Confirmed live: a real scraper-produced export (unlike the older Skillport-UI-exported
            // files this carry-down logic was originally built for) never has a separate
            // username-only header row at all - EVERY row is fully populated, including its own
            // "User ID" cell. Use that row's own identity value directly when present, instead of
            // depending exclusively on the carry-down from a header row this file format doesn't
            // have - dropping every single row for want of one was the actual bug (978 real rows
            // imported as 0). Still falls back to the carried-down value for the older format, where
            // data rows genuinely don't repeat the identity on every row.
            var ownIdentity = cells.TryGetValue("SkillportUserIdText", out var idCell) && !idCell.IsEmpty()
                ? idCell.GetString().Trim()
                : string.Empty;
            if (ownIdentity.Length > 0)
            {
                currentSkillportUsername = ownIdentity;
            }

            if (currentSkillportUsername.Length == 0)
            {
                // A data row appeared before any header row was seen - shouldn't happen in a real
                // export, but skip rather than insert an orphaned row with no identity to attach to.
                continue;
            }

            var dataRow = table.NewRow();
            dataRow["SkillportUsername"] = currentSkillportUsername;
            foreach (var (column, _) in activeColumns)
            {
                dataRow[column] = ConvertValue(column, cells[column]);
            }

            // AssetId/AssetTitle are NOT NULL on the TVP - a row missing either one is malformed
            // (e.g. a row the header-vs-data heuristic above misjudged, or a genuinely incomplete
            // source row) and would otherwise crash the entire batch with a single bad row. Skip
            // it instead of inserting - the rest of a real, mostly-well-formed export still imports.
            if (dataRow["AssetId"] is DBNull || string.IsNullOrWhiteSpace(dataRow["AssetId"] as string)
                || dataRow["AssetTitle"] is DBNull || string.IsNullOrWhiteSpace(dataRow["AssetTitle"] as string))
            {
                continue;
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }

    // A real course/resource title always contains actual words - it's never blank, never a bare
    // number, and never starts with "/" (that's the signature of a Group Path value landing in
    // Asset Title's slot under the wrong candidate layout - see the layout-selection comment above).
    private static bool LooksLikeRealAssetTitle(string text) =>
        !string.IsNullOrWhiteSpace(text) && !text.StartsWith('/') && text.Any(char.IsLetter);

    // Skillsoft's own asset codes always mix letters and underscores (e.g. "bs_ald17_a03_enus",
    // "pit_xk0_005") - a bare number in this slot is always some other field (a score, a count)
    // misread under the wrong candidate layout, never a genuine Asset ID.
    private static bool LooksLikeRealAssetId(string text) =>
        !string.IsNullOrWhiteSpace(text) && text.Any(c => char.IsLetter(c) || c == '_');

    /// <summary>Reads a cell's ACTUAL typed value first (DateTime/TimeSpan) when Excel itself
    /// stored it that way, before ever falling back to parsing its displayed text. Confirmed live:
    /// a real Skillport export has date/duration columns formatted as genuine Excel date/time
    /// cells, not plain text - cell.GetString() on those returns whatever the cell's own display
    /// number format renders (locale/format-dependent), which silently failed every hardcoded
    /// parse pattern below and imported as entirely blank, even though every plain-text/numeric
    /// column on the same row (scores, counts, status) parsed correctly.</summary>
    private static object ConvertValue(string column, IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return DBNull.Value;
        }

        if (DurationColumns.Contains(column))
        {
            if (cell.DataType == XLDataType.TimeSpan)
            {
                var span = cell.GetTimeSpan();
                return (int)span.TotalMinutes;
            }

            var raw = cell.GetString().Trim();
            return ParseDurationToMinutes(raw) is int minutes ? minutes : DBNull.Value;
        }

        if (DateColumns.Contains(column))
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                return cell.GetDateTime().Date;
            }

            var raw = cell.GetString().Trim();
            return ParseDate(raw) is DateOnly date ? date.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        }

        if (DecimalColumns.Contains(column))
        {
            var raw = cell.GetString().Trim();
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : DBNull.Value;
        }

        if (IntColumns.Contains(column))
        {
            var raw = cell.GetString().Trim();
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : DBNull.Value;
        }

        return cell.GetString().Trim();
    }

    /// <summary>"H:MM" or "HH:MM" text (e.g. "0:24", "1:47") to whole minutes. Split manually
    /// rather than TimeSpan.Parse - a cumulative "Absolute" duration can exceed 24 hours, which
    /// TimeSpan's "h:mm" format does not reliably round-trip.</summary>
    private static int? ParseDurationToMinutes(string raw)
    {
        var parts = raw.Split(':');
        if (parts.Length != 2) return null;
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)) return null;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)) return null;
        return hours * 60 + minutes;
    }

    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy", "yyyy/MM/dd"];

    private static DateOnly? ParseDate(string raw)
    {
        if (DateOnly.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose) ? loose : null;
    }
}
