using SkillsetsBackend.Application.RosterImport;
using SkillsetsBackend.Application.RosterImport.Interfaces;

namespace SkillsetsBackend.UnitTests;

/// <summary>The rules that decide what happens to each roster line. These run before anything
/// touches the database, and the preview and the real import share them - so a change here changes
/// both, which is the point.</summary>
public class RosterRowInterpreterTests
{
    private static RosterRawRow Row(
        int rowNumber = 2,
        string? first = "Ada",
        string? last = "Lovelace",
        string? email = "ada@example.test",
        string? phone = null,
        string? company = null,
        string? type = null,
        string? password = null,
        string? mgr = null,
        string? update = null) =>
        new(rowNumber, first, last, email, phone, company, type, password, mgr, update);

    [Fact]
    public void Blank_employee_type_defaults_to_non_it()
    {
        var result = RosterRowInterpreter.Interpret([Row(type: null)]);

        Assert.Equal("NON-IT", result[0].EmployeeType);
    }

    [Theory]
    [InlineData("IT", "IT")]
    [InlineData("it", "IT")]
    [InlineData(" It ", "IT")]
    // Real files use all three of these for the same thing.
    [InlineData("NON", "NON-IT")]
    [InlineData("NON_IT", "NON-IT")]
    [InlineData("NON-IT", "NON-IT")]
    [InlineData("anything else", "NON-IT")]
    public void Employee_type_is_normalized(string raw, string expected)
    {
        var result = RosterRowInterpreter.Interpret([Row(type: raw)]);

        Assert.Equal(expected, result[0].EmployeeType);
    }

    [Theory]
    [InlineData("Yes", true)]
    [InlineData("yes", true)]
    [InlineData("Y", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("No", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("maybe", false)]
    public void Manager_dashboard_only_yes_means_yes(string? raw, bool expected)
    {
        var result = RosterRowInterpreter.Interpret([Row(mgr: raw)]);

        Assert.Equal(expected, result[0].GiveManagerDashboard);
    }

    [Fact]
    public void A_supplied_password_is_used_exactly_as_written()
    {
        // Including a leading zero and a length that isn't 4 - these are credentials people may
        // already hold, so they must not be "corrected".
        var result = RosterRowInterpreter.Interpret([Row(password: "0299")]);

        Assert.Equal("0299", result[0].Password);
    }

    [Fact]
    public void A_missing_password_becomes_a_four_digit_code_never_starting_with_zero()
    {
        // Generated, so assert the rule over many draws rather than one lucky value.
        var rows = Enumerable.Range(2, 300).Select(i => Row(rowNumber: i, email: $"u{i}@example.test")).ToList();

        var result = RosterRowInterpreter.Interpret(rows);

        Assert.All(result, r =>
        {
            Assert.Equal(4, r.Password.Length);
            Assert.All(r.Password, c => Assert.True(char.IsDigit(c)));
            Assert.NotEqual('0', r.Password[0]);
        });
    }

    [Fact]
    public void Missing_email_fails_the_row_with_a_reason_that_names_the_field()
    {
        var result = RosterRowInterpreter.Interpret([Row(email: null)]);

        Assert.Equal(RosterRowVerdict.MissingRequiredField, result[0].Verdict);
        Assert.Contains("Email", result[0].Reason);
    }

    [Fact]
    public void Missing_names_fail_the_row()
    {
        var result = RosterRowInterpreter.Interpret([Row(first: null, last: null)]);

        Assert.Equal(RosterRowVerdict.MissingRequiredField, result[0].Verdict);
        Assert.Contains("First Name", result[0].Reason);
        Assert.Contains("Last Name", result[0].Reason);
    }

    [Fact]
    public void A_malformed_email_is_invalid_rather_than_missing()
    {
        var result = RosterRowInterpreter.Interpret([Row(email: "not-an-email")]);

        Assert.Equal(RosterRowVerdict.Invalid, result[0].Verdict);
        Assert.Contains("not a valid email", result[0].Reason);
    }

    [Fact]
    public void The_second_appearance_of_an_email_points_back_at_the_first()
    {
        var result = RosterRowInterpreter.Interpret(
        [
            Row(rowNumber: 6, email: "dup@example.test"),
            Row(rowNumber: 9, email: "DUP@example.test"), // same person, different case
        ]);

        Assert.Equal(RosterRowVerdict.Valid, result[0].Verdict);
        Assert.Equal(RosterRowVerdict.DuplicateInFile, result[1].Verdict);
        Assert.Contains("row 6", result[1].Reason);
    }

    /// <summary>The template's "UPDATE: Add or Remove" column. Importing a Remove line would create
    /// the very account the file is asking to take off the roster.</summary>
    [Fact]
    public void A_row_marked_remove_is_not_imported()
    {
        var result = RosterRowInterpreter.Interpret([Row(update: "Remove")]);

        Assert.Equal(RosterRowVerdict.MarkedForRemoval, result[0].Verdict);
    }

    [Fact]
    public void A_row_marked_add_is_imported_normally()
    {
        var result = RosterRowInterpreter.Interpret([Row(update: "Add")]);

        Assert.Equal(RosterRowVerdict.Valid, result[0].Verdict);
    }
}
