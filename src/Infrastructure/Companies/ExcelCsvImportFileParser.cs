using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation.Results;
using SkillsetsBackend.Application.Companies.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Infrastructure.Companies;

/// <summary>Reads a Company Import .xlsx (ClosedXML) or .csv (CsvHelper) file into plain
/// ImportRawRow records, matching the 15 expected columns by header name - case-insensitive, trimmed,
/// order-independent. Purely mechanical extraction; all business validation happens in
/// ImportCompaniesCommandHandler.</summary>
public class ExcelCsvImportFileParser : IImportFileParser
{
    private static readonly string[] RequiredHeaders = ["co code", "company name"];

    public IReadOnlyList<ImportRawRow> Parse(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" => ParseExcel(fileStream),
            ".csv" => ParseCsv(fileStream),
            _ => throw new AppValidationException([new ValidationFailure("File", "Only .xlsx and .csv files are supported.")]),
        };
    }

    private static IReadOnlyList<ImportRawRow> ParseExcel(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.Row(1);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        var columnByHeader = new Dictionary<string, int>();
        for (var col = 1; col <= lastColumn; col++)
        {
            var headerText = Normalize(headerRow.Cell(col).GetString());
            if (headerText.Length > 0)
            {
                columnByHeader[headerText] = col;
            }
        }

        EnsureRequiredHeaders(columnByHeader.Keys);

        var rows = new List<ImportRawRow>();
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var rowNumber = 0;

        for (var r = 2; r <= lastRowUsed; r++)
        {
            var xlRow = worksheet.Row(r);
            if (xlRow.IsEmpty())
            {
                continue;
            }

            rowNumber++;
            rows.Add(new ImportRawRow(
                rowNumber,
                GetExcelCell(xlRow, columnByHeader, "co code"),
                GetExcelCell(xlRow, columnByHeader, "company name"),
                GetExcelCell(xlRow, columnByHeader, "expiration date"),
                GetExcelCell(xlRow, columnByHeader, "point of contact"),
                GetExcelCell(xlRow, columnByHeader, "email"),
                GetExcelCell(xlRow, columnByHeader, "phone"),
                GetExcelCell(xlRow, columnByHeader, "street 1"),
                GetExcelCell(xlRow, columnByHeader, "street 2"),
                GetExcelCell(xlRow, columnByHeader, "city"),
                GetExcelCell(xlRow, columnByHeader, "state"),
                GetExcelCell(xlRow, columnByHeader, "zip"),
                GetExcelCell(xlRow, columnByHeader, "payment form"),
                GetExcelCell(xlRow, columnByHeader, "total payment"),
                GetExcelCell(xlRow, columnByHeader, "purchase date"),
                GetExcelCell(xlRow, columnByHeader, "start date")));
        }

        return rows;
    }

    /// <summary>Date-typed Excel cells are re-formatted as dd/MM/yyyy explicitly (rather than trusting
    /// GetString(), which follows whatever display format the cell happens to have) so the handler's
    /// day-first date parsing always sees a consistent, unambiguous format regardless of how the
    /// source spreadsheet formatted the column.</summary>
    private static string? GetExcelCell(IXLRow row, IReadOnlyDictionary<string, int> columnByHeader, string headerKey)
    {
        if (!columnByHeader.TryGetValue(headerKey, out var col))
        {
            return null;
        }

        var cell = row.Cell(col);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            return cell.GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        var text = cell.GetString().Trim();
        return text.Length == 0 ? null : text;
    }

    private static IReadOnlyList<ImportRawRow> ParseCsv(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => Normalize(args.Header),
        };
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        EnsureRequiredHeaders(csv.HeaderRecord?.Select(Normalize) ?? []);

        var rows = new List<ImportRawRow>();
        var rowNumber = 0;

        while (csv.Read())
        {
            if (IsBlankCsvRow(csv))
            {
                continue;
            }

            rowNumber++;
            rows.Add(new ImportRawRow(
                rowNumber,
                GetCsvField(csv, "co code"),
                GetCsvField(csv, "company name"),
                GetCsvField(csv, "expiration date"),
                GetCsvField(csv, "point of contact"),
                GetCsvField(csv, "email"),
                GetCsvField(csv, "phone"),
                GetCsvField(csv, "street 1"),
                GetCsvField(csv, "street 2"),
                GetCsvField(csv, "city"),
                GetCsvField(csv, "state"),
                GetCsvField(csv, "zip"),
                GetCsvField(csv, "payment form"),
                GetCsvField(csv, "total payment"),
                GetCsvField(csv, "purchase date"),
                GetCsvField(csv, "start date")));
        }

        return rows;
    }

    private static bool IsBlankCsvRow(CsvReader csv) =>
        csv.Parser.Record is null || csv.Parser.Record.All(string.IsNullOrWhiteSpace);

    private static string? GetCsvField(CsvReader csv, string headerKey)
    {
        if (!csv.TryGetField<string>(headerKey, out var value))
        {
            return null;
        }

        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static void EnsureRequiredHeaders(IEnumerable<string> normalizedHeaders)
    {
        var present = new HashSet<string>(normalizedHeaders);
        var missing = RequiredHeaders.Where(h => !present.Contains(h)).ToList();
        if (missing.Count > 0)
        {
            throw new AppValidationException([new ValidationFailure("File", $"Missing required column(s): {string.Join(", ", missing)}")]);
        }
    }

    private static string Normalize(string? header) => (header ?? string.Empty).Trim().ToLowerInvariant();
}
