using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using FluentValidation.Results;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Infrastructure.RosterImport;

/// <summary>
/// Reads an employee roster file into plain rows. Every supported format is first flattened into the
/// same rectangular grid of strings, and a single routine then interprets that grid - rather than
/// three near-identical parsers that would drift apart.
///
/// Three real customer files drove this design, and no two agree:
///
///   1. The SkillSets "STUDENT DASHBOARD ROSTER FORM" (.xls and .csv exports of it) puts four rows of
///      logo/instructions ABOVE the header, so the header is on row 5, not row 1. Its company name
///      is not a column at all - it lives in an "Enter Your Organization's Name:" cell above the
///      header and applies to every row. Its headers carry embedded newlines and parenthetical
///      instructions ("GIVE MGR DASHBOARD?\n(FOR GRP REPORTS...)   ENTER YES or NO").
///   2. A plain sheet whose header IS row 1: firstname/lastname/email/phone/password.
///   3. Column ORDER differs between them (the template puts Employee Type before Email).
///
/// So: the header row is DETECTED rather than assumed, and columns are matched by normalized header
/// text rather than by position. Purely mechanical - defaults, duplicates and required-field rules
/// all belong to the handler.
/// </summary>
public class RosterImportFileParser : IRosterImportFileParser
{
    /// <summary>Matches the Company Import tool's ceiling. Anything larger is a data-loading job,
    /// not an admin-screen upload.</summary>
    private const int MaxDataRows = 20_000;

    /// <summary>How far down to look for the header before giving up. The SkillSets template needs
    /// 5; the allowance is for future variants with a taller preamble.</summary>
    private const int MaxHeaderScanRows = 25;

    static RosterImportFileParser()
    {
        // ExcelDataReader reads legacy BIFF .xls, which stores text in a Windows code page rather
        // than UTF-8; without this registration it throws on the first non-ASCII cell.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static readonly Dictionary<string, string[]> FieldSynonyms = new()
    {
        ["FirstName"] = ["first name", "firstname", "first", "fname", "given name"],
        ["LastName"] = ["last name", "lastname", "last", "lname", "surname", "family name"],
        ["Email"] = ["email", "email address", "e mail", "emailaddress", "mail"],
        ["Phone"] = ["direct phone number", "phone number", "phone", "telephone", "mobile", "contact number", "cell"],
        ["CompanyName"] = ["company", "company name", "companyname", "organization", "organisation", "org", "organization name"],
        ["EmployeeType"] = [
            "is this person it or non it", "employee type", "employeetype", "user type", "type",
            "it or non it", "it non it", "it or non-it",
        ],
        ["Password"] = ["password", "pass", "pwd", "temp password", "initial password"],
        ["GiveManagerDashboard"] = [
            "give mgr dashboard", "give manager dashboard", "mgr dashboard", "manager dashboard",
            "give mgr dashboard?", "manager access", "is manager",
        ],
        ["UpdateAction"] = ["update", "add or remove", "update add or remove", "action"],
    };

    private static readonly Regex OrganizationLabel =
        new(@"organi[sz]ation'?s?\s+name", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RosterParseResult Parse(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var grid = extension switch
        {
            ".xlsx" => ReadXlsx(fileStream),
            ".xls" => ReadLegacyXls(fileStream),
            ".csv" => ReadCsv(fileStream),
            _ => throw new AppValidationException([new ValidationFailure("File",
                "Only .xlsx, .xls and .csv files are supported.")]),
        };

        return Interpret(grid);
    }

    // ----- format readers: each produces the same rectangular grid -----

    private static List<string?[]> ReadXlsx(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        var grid = new List<string?[]>(lastRow);
        for (var r = 1; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            var cells = new string?[lastColumn];
            for (var c = 1; c <= lastColumn; c++)
            {
                cells[c - 1] = CellText(row.Cell(c));
            }

            grid.Add(cells);
        }

        return grid;
    }

    /// <summary>Numeric cells are rendered without scientific notation or a trailing ".0" - phone
    /// numbers and all-digit passwords routinely arrive typed as numbers, and "4.34242E+09" or
    /// "4817.0" would both be wrong.</summary>
    private static string? CellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        var text = cell.DataType switch
        {
            XLDataType.DateTime => cell.GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            XLDataType.Number => FormatNumber(cell.GetDouble()),
            _ => cell.GetString(),
        };

        text = text.Trim();
        return text.Length == 0 ? null : text;
    }

    private static string FormatNumber(double value) =>
        value == Math.Floor(value) && Math.Abs(value) < 1e15
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.################", CultureInfo.InvariantCulture);

    /// <summary>Legacy BIFF .xls - the format the customer's own template is saved in. ClosedXML
    /// handles only the OOXML .xlsx container, so this path uses ExcelDataReader instead.</summary>
    private static List<string?[]> ReadLegacyXls(Stream fileStream)
    {
        using var reader = ExcelReaderFactory.CreateBinaryReader(fileStream);
        var grid = new List<string?[]>();

        do
        {
            while (reader.Read())
            {
                var cells = new string?[reader.FieldCount];
                for (var c = 0; c < reader.FieldCount; c++)
                {
                    cells[c] = ReaderCellText(reader, c);
                }

                grid.Add(cells);
            }

            // Only the first worksheet is the roster; the template's later sheets are instructions.
            break;
        }
        while (reader.NextResult());

        return grid;
    }

    private static string? ReaderCellText(IExcelDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        var text = value switch
        {
            DateTime date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            double number => FormatNumber(number),
            _ => value?.ToString() ?? string.Empty,
        };

        text = text.Trim();
        return text.Length == 0 ? null : text;
    }

    /// <summary>Read positionally, NOT through CsvHelper's header binding: the SkillSets CSV export
    /// carries the same four-row preamble as the spreadsheet, so its first line is a logo caption,
    /// not a header.</summary>
    private static List<string?[]> ReadCsv(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            BadDataFound = null,
            MissingFieldFound = null,
            DetectColumnCountChanges = false,
        };

        using var csv = new CsvReader(reader, config);
        var grid = new List<string?[]>();

        while (csv.Read())
        {
            var record = csv.Parser.Record;
            if (record is null)
            {
                continue;
            }

            grid.Add(record.Select(v => string.IsNullOrWhiteSpace(v) ? null : v.Trim()).ToArray());
        }

        return grid;
    }

    // ----- interpretation -----

    private static RosterParseResult Interpret(List<string?[]> grid)
    {
        if (grid.Count == 0 || grid.All(row => row.All(string.IsNullOrWhiteSpace)))
        {
            throw Reject("This file is empty.");
        }

        var headerIndex = FindHeaderRow(grid);
        if (headerIndex < 0)
        {
            throw Reject("No column titles were found. The file needs a row of headings that "
                         + "includes at least First Name and Email (a title block above it is fine).");
        }

        var header = grid[headerIndex];
        var columnByField = MapColumns(header);
        var mappedColumns = BuildMappedColumnReport(header, columnByField);

        if (!columnByField.ContainsKey("Email"))
        {
            throw Reject("There is no Email column, so no accounts can be created. The headings "
                         + $"found were: {DescribeHeaders(header)}.");
        }

        var organizationName = FindOrganizationName(grid, headerIndex);

        var rows = new List<RosterRawRow>();
        for (var i = headerIndex + 1; i < grid.Count; i++)
        {
            var cells = grid[i];
            if (cells.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            // The file's own line number (1-based), so a reported row matches what Excel shows.
            rows.Add(new RosterRawRow(
                i + 1,
                Get(cells, columnByField, "FirstName"),
                Get(cells, columnByField, "LastName"),
                Get(cells, columnByField, "Email"),
                Get(cells, columnByField, "Phone"),
                Get(cells, columnByField, "CompanyName"),
                Get(cells, columnByField, "EmployeeType"),
                Get(cells, columnByField, "Password"),
                Get(cells, columnByField, "GiveManagerDashboard"),
                Get(cells, columnByField, "UpdateAction")));

            if (rows.Count > MaxDataRows)
            {
                throw new AppValidationException([new ValidationFailure("File",
                    $"This file has more than {MaxDataRows:N0} rows - please split it into smaller files.")]);
            }
        }

        // Headings but no people. Almost always the wrong file (a blank template, or a report that
        // happens to have a First Name column) - importing "successfully" with nothing to show is
        // more confusing than being told the file is unusable.
        if (rows.Count == 0)
        {
            throw Reject($"The column titles were found on row {headerIndex + 1}, but there are no "
                         + "data rows underneath them.");
        }

        // Every row missing an email means the Email column was matched to the wrong thing, or this
        // is not a roster at all. Individual blanks are a per-row problem; ALL of them is a format
        // problem, and saying so here is far clearer than 572 identical row errors.
        if (rows.All(r => string.IsNullOrWhiteSpace(r.Email)))
        {
            throw Reject($"None of the {rows.Count} rows has an email address, so no accounts can be "
                         + "created. Check that the Email column is filled in.");
        }

        return new RosterParseResult(rows, organizationName, headerIndex + 1, mappedColumns);
    }

    /// <summary>Every rejection ends with the same instruction, because in every one of these cases
    /// the fix is the same: start from a file shaped the way the importer expects.</summary>
    private static AppValidationException Reject(string problem) =>
        new([new ValidationFailure("File",
            $"{problem} Download the sample template, put your data into it, and upload that.")]);

    private static string DescribeHeaders(string?[] header)
    {
        var found = header
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h!.Split('\n', '\r')[0].Trim())
            .Take(10)
            .ToList();

        return found.Count == 0 ? "(none)" : string.Join(", ", found);
    }

    /// <summary>The header is the first row that maps at least two known fields AND includes one of
    /// the two that every roster must have. Two matches alone is too weak - the template's
    /// instruction block contains prose that can incidentally contain a field word.</summary>
    private static int FindHeaderRow(List<string?[]> grid)
    {
        var limit = Math.Min(grid.Count, MaxHeaderScanRows);
        for (var i = 0; i < limit; i++)
        {
            var mapped = MapColumns(grid[i]);
            if (mapped.Count >= 2 && (mapped.ContainsKey("Email") || mapped.ContainsKey("FirstName")))
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> MapColumns(string?[] header)
    {
        var result = new Dictionary<string, int>();
        for (var c = 0; c < header.Length; c++)
        {
            var normalized = NormalizeHeader(header[c]);
            if (normalized.Length == 0)
            {
                continue;
            }

            foreach (var (field, synonyms) in FieldSynonyms)
            {
                // First column wins: a later "Type" column must not steal the mapping from an
                // earlier, more specific "Employee Type".
                if (result.ContainsKey(field))
                {
                    continue;
                }

                if (synonyms.Contains(normalized))
                {
                    result[field] = c;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Header cells in the real template are multi-line instructions, e.g.
    ///   "IS THIS PERSON IT OR NON-IT?\n(FOR SEPARATED GROUP REPORTS)\nENTER IT or NON-IT"
    /// Only the first line is the actual title, so everything from the first newline or opening
    /// bracket onwards is dropped, then punctuation is flattened to single spaces.
    /// </summary>
    private static string NormalizeHeader(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var text = raw;
        var cut = text.IndexOfAny(['\n', '\r', '(']);
        if (cut > 0)
        {
            text = text[..cut];
        }

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>The SkillSets template states the company once, above the table, as
    /// "Enter Your Organization's Name:" followed by the value in a nearby cell to the right.</summary>
    private static string? FindOrganizationName(List<string?[]> grid, int headerIndex)
    {
        for (var i = 0; i < headerIndex; i++)
        {
            var cells = grid[i];
            for (var c = 0; c < cells.Length; c++)
            {
                if (cells[c] is null || !OrganizationLabel.IsMatch(cells[c]!))
                {
                    continue;
                }

                for (var v = c + 1; v < cells.Length; v++)
                {
                    if (!string.IsNullOrWhiteSpace(cells[v]))
                    {
                        return cells[v]!.Trim();
                    }
                }
            }
        }

        return null;
    }

    private static Dictionary<string, string> BuildMappedColumnReport(string?[] header, Dictionary<string, int> columnByField)
    {
        var report = new Dictionary<string, string>();
        foreach (var (field, index) in columnByField)
        {
            var raw = header[index] ?? string.Empty;
            // Collapse the multi-line instruction text so the preview shows a readable title.
            var firstLine = raw.Split('\n', '\r').FirstOrDefault()?.Trim() ?? raw;
            report[field] = firstLine;
        }

        return report;
    }

    private static string? Get(string?[] cells, Dictionary<string, int> columnByField, string field)
    {
        if (!columnByField.TryGetValue(field, out var index) || index >= cells.Length)
        {
            return null;
        }

        var value = cells[index];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
