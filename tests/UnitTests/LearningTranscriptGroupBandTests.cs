using System.Data;
using System.Reflection;
using ClosedXML.Excel;
using SkillsetsBackend.Infrastructure.LearningTranscript;

namespace SkillsetsBackend.UnitTests;

/// <summary>
/// Proves the exact pattern the user showed in a screenshot: a one-cell "group band" row holding
/// only the person's real Skillport username (e.g. "1lc323177"), followed by a VARYING number of
/// detail rows (1, then 2, then 3) that each carry a numeric "User ID" instead - and confirms every
/// one of those detail rows is attributed to the band above it, not just the first.
///
/// Calls ParseWorkbookIntoTable directly via reflection - the actual private method the real import
/// uses, not a re-implementation of its logic - so this is proof against production code, not an
/// assurance about it.
/// </summary>
public class LearningTranscriptGroupBandTests
{
    private static DataTable Parse(byte[] xlsxBytes)
    {
        var method = typeof(LearningTranscriptImportService)
            .GetMethod("ParseWorkbookIntoTable", BindingFlags.NonPublic | BindingFlags.Static)!;

        using var stream = new MemoryStream(xlsxBytes);
        return (DataTable)method.Invoke(null, [stream])!;
    }

    /// <summary>Builds a workbook shaped exactly like the user's screenshot: a real header row, then
    /// bands each followed by however many detail rows are requested.</summary>
    private static byte[] BuildWorkbook(params (string Band, (string UserId, string First, string Last, string Group)[] Details)[] groups)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");

        string[] headers =
        [
            "User ID", "First Name", "Last Name", "Display First Name", "Display Last Name",
            "Location", "User Status", "Group Name", "Group Org Code", "Group Path",
            "Asset Title", "Asset ID", "Asset Type", "Asset Sub-type",
        ];
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        var r = 2;
        foreach (var (band, details) in groups)
        {
            // The band row: ONLY column A has a value - exactly what the real export does.
            sheet.Cell(r, 1).Value = band;
            r++;

            foreach (var (userId, first, last, group) in details)
            {
                sheet.Cell(r, 1).Value = userId;
                sheet.Cell(r, 2).Value = first;
                sheet.Cell(r, 3).Value = last;
                sheet.Cell(r, 4).Value = first;
                sheet.Cell(r, 5).Value = last;
                sheet.Cell(r, 7).Value = "Activated";
                sheet.Cell(r, 8).Value = group;
                sheet.Cell(r, 9).Value = group;
                sheet.Cell(r, 11).Value = "Some Course " + r;
                sheet.Cell(r, 12).Value = "asset_" + r;
                sheet.Cell(r, 13).Value = "Courses";
                sheet.Cell(r, 14).Value = "Courses";
                r++;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static List<(string SkillportUsername, string FirstName, string LastName)> RowsOf(DataTable table) =>
        table.Rows.Cast<DataRow>()
            .Select(row => ((string)row["SkillportUsername"], (string)row["FirstName"], (string)row["LastName"]))
            .ToList();

    /// <summary>Exactly the screenshot: bands with 1, 2, and 3 detail rows respectively. Every detail
    /// row must carry the band directly above it, including the 2nd and 3rd rows under a band - the
    /// case a naive "use the band only for the very next row" implementation would get wrong.</summary>
    [Fact]
    public void A_band_row_is_carried_onto_every_detail_row_beneath_it_not_just_the_first()
    {
        var bytes = BuildWorkbook(
            ("1lc323151", [("507613", "Cristi", "Tam", "LC_CORA")]),
            ("1lc323170", [("507632", "Taka", "Nash", "LC_GROUPO"), ("507632", "Taka", "Nash", "LC_GROUPO")]),
            ("1lc323177", [
                ("507640", "Cristina", "Perez", "LC_IRONWOMAN"),
                ("507640", "Cristina", "Perez", "LC_IRONWOMAN"),
                ("507640", "Cristina", "Perez", "LC_IRONWOMAN"),
            ]));

        var rows = RowsOf(Parse(bytes));

        Assert.Equal(6, rows.Count);

        Assert.Equal(("1lc323151", "Cristi", "Tam"), rows[0]);

        Assert.Equal(("1lc323170", "Taka", "Nash"), rows[1]);
        Assert.Equal(("1lc323170", "Taka", "Nash"), rows[2]);

        Assert.Equal(("1lc323177", "Cristina", "Perez"), rows[3]);
        Assert.Equal(("1lc323177", "Cristina", "Perez"), rows[4]);
        Assert.Equal(("1lc323177", "Cristina", "Perez"), rows[5]);
    }

    /// <summary>The numeric "User ID" column (e.g. "507640") must never be used as the identity when
    /// a real band value is available above it - that numeric id resolves against nothing in this
    /// system and was the exact bug fixed earlier (630 of 658 identities keyed wrong).</summary>
    [Fact]
    public void The_numeric_user_id_column_is_never_used_as_the_identity_when_a_band_is_present()
    {
        var bytes = BuildWorkbook(("1lc323177", [("507640", "Cristina", "Perez", "LC_IRONWOMAN")]));

        var rows = RowsOf(Parse(bytes));

        Assert.Single(rows);
        Assert.Equal("1lc323177", rows[0].SkillportUsername);
        Assert.NotEqual("507640", rows[0].SkillportUsername);
    }

    /// <summary>A new band correctly replaces the previous one for what follows it - rows never
    /// leak forward into the wrong person's block.</summary>
    [Fact]
    public void A_new_band_stops_the_previous_one_from_leaking_forward()
    {
        var bytes = BuildWorkbook(
            ("1lc111111", [("100001", "Alice", "Anders", "LC_A")]),
            ("1lc222222", [("100002", "Bob", "Baker", "LC_B")]));

        var rows = RowsOf(Parse(bytes));

        Assert.Equal("1lc111111", rows[0].SkillportUsername);
        Assert.Equal("1lc222222", rows[1].SkillportUsername);
    }
}
