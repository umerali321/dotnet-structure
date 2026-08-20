namespace SkillsetsBackend.Application.Companies.Interfaces;

/// <summary>One row of the uploaded import file, exactly as read - no parsing/validation/trimming
/// happens here, that's ImportCompaniesCommandHandler's job. RowNumber is 1-based and counts from the
/// first data row (i.e. excludes the header row), matching what a spreadsheet user would call "row 2,
/// row 3, ..." when the header is row 1.</summary>
public record ImportRawRow(
    int RowNumber,
    string? CoCode,
    string? CompanyName,
    string? ExpirationDate,
    string? PointOfContact,
    string? Email,
    string? Phone,
    string? Street1,
    string? Street2,
    string? City,
    string? State,
    string? Zip,
    string? PaymentForm,
    string? TotalPayment,
    string? PurchaseDate,
    string? StartDate);

/// <summary>Reads an uploaded .xlsx or .csv Company Import file into plain rows, matching columns by
/// header name (case-insensitive, trimmed, order-independent). Lives behind this interface so the
/// Application layer never depends on the actual parsing library (ClosedXML/CsvHelper - see
/// Infrastructure/Companies/ExcelCsvImportFileParser.cs).</summary>
public interface IImportFileParser
{
    /// <summary>Throws SkillsetsBackend.Application.Common.Exceptions.ValidationException if the file
    /// extension isn't supported or the header row is missing a column this tool can't function
    /// without (Co Code, Company Name) - a structural problem, reported before any row is processed.</summary>
    IReadOnlyList<ImportRawRow> Parse(Stream fileStream, string fileName);
}
