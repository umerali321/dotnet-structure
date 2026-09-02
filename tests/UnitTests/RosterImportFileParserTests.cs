using System.Text;
using ClosedXML.Excel;
using SkillsetsBackend.Infrastructure.RosterImport;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.UnitTests;

/// <summary>
/// The parser's whole job is coping with roster files that disagree with each other, so these
/// fixtures reproduce the STRUCTURE of the real customer files (title block above the header, the
/// organization name in a cell rather than a column, multi-line header text, differing column
/// order) with invented names - the real files contain live customer email addresses and don't
/// belong in the repository.
/// </summary>
public class RosterImportFileParserTests
{
    private readonly RosterImportFileParser _parser = new();

    /// <summary>The SkillSets "STUDENT DASHBOARD ROSTER FORM": four rows of branding and
    /// instructions, the company in an "Enter Your Organization's Name:" cell, then a header whose
    /// cells carry embedded newlines - and Employee Type sitting BEFORE Email.</summary>
    private static MemoryStream BuildSkillSetsTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("UploadStudentRoster");

        sheet.Cell(1, 3).Value = "SkillSets Online\n       STUDENT DASHBOARD ROSTER FORM";
        sheet.Cell(2, 1).Value = "PLEASE READ FIRST:";
        sheet.Cell(3, 1).Value = "PLEASE NOTE: All information you provide is confidential.";
        sheet.Cell(4, 1).Value = "Enter Your Organization's Name:";
        sheet.Cell(4, 3).Value = "CommunityCare";

        sheet.Cell(5, 1).Value = "FIRST NAME";
        sheet.Cell(5, 2).Value = "LAST NAME";
        sheet.Cell(5, 3).Value = "IS THIS PERSON IT OR NON-IT?\n(FOR SEPARATED GROUP REPORTS)\nENTER IT or NON-IT";
        sheet.Cell(5, 4).Value = "EMAIL";
        sheet.Cell(5, 5).Value = "DIRECT PHONE NUMBER";
        sheet.Cell(5, 6).Value = "GIVE MGR DASHBOARD?\n(FOR GRP REPORTS AND TO UPDATE ROSTER)       ENTER YES or NO";
        sheet.Cell(5, 7).Value = "UPDATE:\nAdd or Remove";

        sheet.Cell(6, 1).Value = "Ada";
        sheet.Cell(6, 2).Value = "Lovelace";
        sheet.Cell(6, 3).Value = "NON_IT";
        sheet.Cell(6, 4).Value = "ada@example.test";
        sheet.Cell(6, 6).Value = "No";
        sheet.Cell(6, 7).Value = "Add";

        sheet.Cell(7, 1).Value = "Grace";
        sheet.Cell(7, 2).Value = "Hopper";
        sheet.Cell(7, 3).Value = "IT";
        sheet.Cell(7, 4).Value = "grace@example.test";
        sheet.Cell(7, 5).Value = 4342420853L; // typed as a NUMBER, as real exports do
        sheet.Cell(7, 6).Value = "Yes";
        sheet.Cell(7, 7).Value = "Add";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Finds_header_below_a_title_block()
    {
        using var file = BuildSkillSetsTemplate();

        var result = _parser.Parse(file, "roster.xlsx");

        Assert.Equal(5, result.DetectedHeaderRowNumber);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public void Reads_the_company_from_the_organization_name_cell_not_a_column()
    {
        using var file = BuildSkillSetsTemplate();

        var result = _parser.Parse(file, "roster.xlsx");

        Assert.Equal("CommunityCare", result.OrganizationName);
    }

    [Fact]
    public void Maps_columns_by_header_text_so_order_does_not_matter()
    {
        using var file = BuildSkillSetsTemplate();

        var result = _parser.Parse(file, "roster.xlsx");

        // Employee Type is column 3 and Email column 4 in this layout - the reverse of the order the
        // simple template uses. Both must land in the right field.
        var ada = result.Rows[0];
        Assert.Equal("Ada", ada.FirstName);
        Assert.Equal("Lovelace", ada.LastName);
        Assert.Equal("ada@example.test", ada.Email);
        Assert.Equal("NON_IT", ada.EmployeeType);
        Assert.Equal("No", ada.GiveManagerDashboard);
        Assert.Equal("Add", ada.UpdateAction);
    }

    [Fact]
    public void Row_numbers_match_the_lines_the_admin_sees_in_excel()
    {
        using var file = BuildSkillSetsTemplate();

        var result = _parser.Parse(file, "roster.xlsx");

        // Data starts on line 6 of the sheet, not line 1 of the data.
        Assert.Equal(6, result.Rows[0].RowNumber);
        Assert.Equal(7, result.Rows[1].RowNumber);
    }

    [Fact]
    public void Numeric_phone_cells_do_not_come_back_in_scientific_notation()
    {
        using var file = BuildSkillSetsTemplate();

        var result = _parser.Parse(file, "roster.xlsx");

        Assert.Equal("4342420853", result.Rows[1].Phone);
    }

    [Fact]
    public void Reads_a_plain_sheet_whose_header_is_the_very_first_row()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "firstname";
        sheet.Cell(1, 2).Value = "lastname";
        sheet.Cell(1, 3).Value = "email";
        sheet.Cell(1, 4).Value = "phone";
        sheet.Cell(1, 5).Value = "password";
        sheet.Cell(2, 1).Value = "Alan";
        sheet.Cell(2, 2).Value = "Turing";
        sheet.Cell(2, 3).Value = "alan@example.test";
        sheet.Cell(2, 4).Value = "4342420853";
        sheet.Cell(2, 5).Value = "4817";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = _parser.Parse(stream, "simple.xlsx");

        Assert.Equal(1, result.DetectedHeaderRowNumber);
        Assert.Null(result.OrganizationName);
        Assert.Equal("alan@example.test", result.Rows[0].Email);
        Assert.Equal("4817", result.Rows[0].Password);
    }

    /// <summary>A leading-zero password like "0299" is real data in the customer's own file. It must
    /// survive as text - read as a number it would become "299".</summary>
    [Fact]
    public void Preserves_a_leading_zero_password_stored_as_text()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "firstname";
        sheet.Cell(1, 2).Value = "email";
        sheet.Cell(1, 3).Value = "password";
        sheet.Cell(2, 1).Value = "Ash";
        sheet.Cell(2, 2).Value = "ash@example.test";
        sheet.Cell(2, 3).SetValue("0299");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = _parser.Parse(stream, "pw.xlsx");

        Assert.Equal("0299", result.Rows[0].Password);
    }

    [Fact]
    public void Reads_a_csv_that_carries_the_same_title_block()
    {
        var csv = string.Join("\n",
        [
            ",\"(925) 964-0531\",\"SkillSets Online\",,,,",
            "PLEASE READ FIRST:,,,,,,",
            "\"PLEASE NOTE: confidential.\",,,,,,",
            "Enter Your Organization's Name:,,Albemarle County Public Schools,,,,",
            "FIRST NAME,LAST NAME,\"IS THIS PERSON IT OR NON-IT?\",EMAIL,DIRECT PHONE NUMBER,\"GIVE MGR DASHBOARD?\",\"UPDATE:\"",
            "Alan,Wright,IT,awright@example.test,4342420853,No,Add",
            "Alfred,Toole,NON,atoole@example.test,4342498702,Yes,Add",
        ]);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.Parse(stream, "roster.csv");

        Assert.Equal("Albemarle County Public Schools", result.OrganizationName);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("NON", result.Rows[1].EmployeeType);
        Assert.Equal("Yes", result.Rows[1].GiveManagerDashboard);
    }

    [Fact]
    public void Skips_blank_rows_between_records()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "first name";
        sheet.Cell(1, 2).Value = "email";
        sheet.Cell(2, 1).Value = "A";
        sheet.Cell(2, 2).Value = "a@example.test";
        sheet.Cell(4, 1).Value = "B";
        sheet.Cell(4, 2).Value = "b@example.test";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = _parser.Parse(stream, "gaps.xlsx");

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(4, result.Rows[1].RowNumber);
    }

    [Fact]
    public void Rejects_a_file_with_no_email_column()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "first name";
        sheet.Cell(1, 2).Value = "last name";
        sheet.Cell(2, 1).Value = "A";
        sheet.Cell(2, 2).Value = "B";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<AppValidationException>(() => _parser.Parse(stream, "no-email.xlsx"));
        Assert.Contains("Email", string.Join(" ", error.Errors["File"]));
    }

    /// <summary>Headings but no people - almost always the wrong file, and "imported 0 rows
    /// successfully" would hide that.</summary>
    [Fact]
    public void Rejects_a_file_with_headers_but_no_data_rows()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "First Name";
        sheet.Cell(1, 2).Value = "Last Name";
        sheet.Cell(1, 3).Value = "Email";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<AppValidationException>(() => _parser.Parse(stream, "empty.xlsx"));
        var message = string.Join(" ", error.Errors["File"]);
        Assert.Contains("no data rows", message);
        Assert.Contains("sample template", message);
    }

    [Fact]
    public void Rejects_a_completely_empty_file()
    {
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Sheet1");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<AppValidationException>(() => _parser.Parse(stream, "blank.xlsx"));
        Assert.Contains("empty", string.Join(" ", error.Errors["File"]));
    }

    /// <summary>One blank email is a row problem; every row blank is a format problem, and saying so
    /// once beats repeating the same error 572 times.</summary>
    [Fact]
    public void Rejects_a_file_where_no_row_has_an_email()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "First Name";
        sheet.Cell(1, 2).Value = "Email";
        sheet.Cell(2, 1).Value = "A";
        sheet.Cell(3, 1).Value = "B";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<AppValidationException>(() => _parser.Parse(stream, "no-emails.xlsx"));
        Assert.Contains("None of the 2 rows has an email", string.Join(" ", error.Errors["File"]));
    }

    [Fact]
    public void Rejection_messages_point_at_the_sample_template()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        sheet.Cell(1, 1).Value = "Widget";
        sheet.Cell(1, 2).Value = "Quantity";
        sheet.Cell(2, 1).Value = "Bolt";
        sheet.Cell(2, 2).Value = 4;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<AppValidationException>(() => _parser.Parse(stream, "not-a-roster.xlsx"));
        Assert.Contains("Download the sample template", string.Join(" ", error.Errors["File"]));
    }

    [Fact]
    public void Rejects_an_unsupported_extension()
    {
        using var stream = new MemoryStream([1, 2, 3]);

        var error = Assert.Throws<AppValidationException>(() => _parser.Parse(stream, "roster.pdf"));
        Assert.Contains(".xlsx", string.Join(" ", error.Errors["File"]));
    }
}
