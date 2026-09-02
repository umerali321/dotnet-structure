using System.Reflection;
using SkillsetsBackend.Application.Companies.Commands.CreateCompany;

namespace SkillsetsBackend.UnitTests;

/// <summary>
/// The Point-of-Contact welcome email must be strictly opt-in. Emailing someone their password is
/// outward-facing and irreversible, so the failure that matters is sending one nobody asked for -
/// which is what a defaulted-on flag would do to every caller that predates it.
/// </summary>
public class CreateCompanyWelcomeEmailTests
{
    [Fact]
    public void The_flag_defaults_to_not_sending()
    {
        // Built the way an older caller would - without mentioning the flag at all.
        var command = new CreateCompanyCommand(
            CompanyCode: "LC_TEST",
            CompanyName: "Test Company",
            CompanyEmail: null,
            CompanyPhone: null,
            AdminFirstName: "Jane",
            AdminLastName: "Doe",
            AdminEmail: "jane@example.com",
            AdminUsername: "jane@example.com",
            AdminPassword: "1234",
            PlanType: "Trial",
            LicenseStartDate: null,
            LicenseEndDate: null);

        Assert.False(command.SendWelcomeEmailToPointOfContact);
    }

    [Fact]
    public void The_flag_can_be_turned_on()
    {
        var command = new CreateCompanyCommand(
            "LC_TEST", "Test Company", null, null, "Jane", "Doe",
            "jane@example.com", "jane@example.com", "1234", "Trial", null, null)
        {
            SendWelcomeEmailToPointOfContact = true,
        };

        Assert.True(command.SendWelcomeEmailToPointOfContact);
    }

    /// <summary>The send must happen AFTER the company row exists, and must be conditional. A send
    /// before CreateCompanyWithAdminAsync would mail credentials for an account that then failed to
    /// be created.</summary>
    [Fact]
    public void The_email_is_sent_only_after_creation_and_only_when_asked()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "Application", "Companies", "Commands", "CreateCompany",
            "CreateCompanyCommandHandler.cs"));

        var creationIndex = source.IndexOf("CreateCompanyWithAdminAsync", StringComparison.Ordinal);
        var guardIndex = source.IndexOf("if (command.SendWelcomeEmailToPointOfContact)", StringComparison.Ordinal);
        var sendIndex = source.IndexOf("_welcomeEmail.SendAsync", StringComparison.Ordinal);

        Assert.True(creationIndex > 0, "company creation call not found");
        Assert.True(guardIndex > creationIndex, "the send is not guarded, or runs before creation");
        Assert.True(sendIndex > guardIndex, "the send is not inside the guard");

        // Reuses the shared welcome email rather than a second mail path, so SMTP settings,
        // template and Email History all still apply.
        Assert.Contains("AccountWelcomeEmail", source);

        // The password mailed must be the one that was actually stored. Clamped - the send is near
        // the end of the file, so a fixed-width slice can run off it.
        var sendCall = source[sendIndex..Math.Min(sendIndex + 200, source.Length)];
        Assert.Contains("command.AdminPassword", sendCall);
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
