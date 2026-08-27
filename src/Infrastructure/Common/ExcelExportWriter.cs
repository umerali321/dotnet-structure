using ClosedXML.Excel;

namespace SkillsetsBackend.Infrastructure.Common;

/// <summary>Builds a simple, single-sheet .xlsx file from a header row and pre-formatted string rows -
/// shared by every "Export to Excel" list action (Employees, Managers/Company Admins, Companies).
/// Values are already display-ready strings by the time they reach here, matching what the on-screen
/// grid shows - this writer has no per-entity knowledge.</summary>
public static class ExcelExportWriter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Write(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < headers.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Count; col++)
            {
                worksheet.Cell(rowIndex, col + 1).Value = row[col];
            }
            rowIndex++;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
