using System.Reflection;
using SkillsetsBackend.Infrastructure.Skillsoft;

namespace SkillsetsBackend.UnitTests;

/// <summary>
/// A user could not start a course because their company name was 51+ characters:
///   "CompanyName is too long for Skillport (max 50 characters)."
/// The limit was fictional. dbo.ActiveLibraryCards.Company_Name is nvarchar(100) - the code applied
/// a flat 50 to every column when the widths differ.
/// </summary>
public class SkillportFieldLengthTests
{
    private static int Constant(string name) =>
        (int)typeof(SkillsoftProvisioningService)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue()!;

    private static string? Fit(string? value, int max) =>
        (string?)typeof(SkillsoftProvisioningService)
            .GetMethod("Fit", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [value, max]);

    /// <summary>The value that matters: it must match the real column, not the old flat 50.</summary>
    [Fact]
    public void CompanyName_limit_matches_the_real_column_width()
    {
        Assert.Equal(100, Constant("CompanyNameMaxLength"));
    }

    [Theory]
    [InlineData("CompanyCodeMaxLength", 50)]
    [InlineData("ManagerIdMaxLength", 50)]
    [InlineData("NameMaxLength", 50)]
    [InlineData("EmailMaxLength", 50)]
    [InlineData("UserIdMaxLength", 50)]
    [InlineData("PasswordMaxLength", 50)]
    [InlineData("FdmNameMaxLength", 50)]
    public void Other_limits_match_their_columns(string constant, int expected)
    {
        Assert.Equal(expected, Constant(constant));
    }

    /// <summary>A 60-character company name is well within nvarchar(100) and must simply pass.</summary>
    [Fact]
    public void A_sixty_character_company_name_is_not_shortened()
    {
        var name = new string('A', 60);

        Assert.Equal(name, Fit(name, Constant("CompanyNameMaxLength")));
    }

    [Fact]
    public void A_company_name_beyond_the_column_is_trimmed_to_fit()
    {
        var name = new string('A', 140);

        var result = Fit(name, Constant("CompanyNameMaxLength"));

        Assert.Equal(100, result!.Length);
        Assert.StartsWith("AAA", result);
    }

    [Fact]
    public void Fit_leaves_short_values_and_nulls_alone()
    {
        Assert.Equal("Acme Corp", Fit("Acme Corp", 50));
        Assert.Null(Fit(null, 50));
        Assert.Equal(string.Empty, Fit(string.Empty, 50));
    }

    /// <summary>Matching keys must never be silently shortened - a truncated Company Code or Email
    /// stops matching the entitlement it was written for, which fails invisibly. Those are rejected
    /// instead, so only the display-only fields are trimmed.</summary>
    [Fact]
    public void Only_display_fields_are_trimmed_matching_keys_are_rejected()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "Infrastructure", "Skillsoft", "SkillsoftProvisioningService.cs"));

        // Rejected (exact-match keys).
        Assert.Contains("nameof(company.CompanyCode), company.CompanyCode, CompanyCodeMaxLength", source);
        Assert.Contains("nameof(request.Email), request.Email, EmailMaxLength", source);
        Assert.Contains("nameof(request.Username), request.Username, UserIdMaxLength", source);

        // Trimmed (display only).
        Assert.Contains("var companyName = Fit(company.CompanyName, CompanyNameMaxLength);", source);
        Assert.Contains("var firstName = Fit(request.FirstName, NameMaxLength);", source);

        // CompanyName must no longer appear in the rejection list at all.
        var rejectionBlock = source[source.IndexOf("var oversizedKey", StringComparison.Ordinal)..
                                    source.IndexOf("var companyName =", StringComparison.Ordinal)];
        Assert.DoesNotContain("CompanyName", rejectionBlock);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }

        return dir!.FullName;
    }
}
