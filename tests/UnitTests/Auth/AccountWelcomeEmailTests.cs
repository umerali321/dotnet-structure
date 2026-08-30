using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Auth.Interfaces;

namespace SkillsetsBackend.UnitTests.Auth;

public class AccountWelcomeEmailTests
{
    [Fact]
    public async Task SendsTheCredentialsThatWereActuallyAssigned()
    {
        var sender = new FakeSender();
        await new AccountWelcomeEmail(sender).SendAsync("new.hire@example.com", "Umer", "4821");

        var send = Assert.Single(sender.Sends);
        Assert.Equal("new.hire@example.com", send.To);
        Assert.Equal("AccountCreated", send.Purpose);
        Assert.Contains("4821", send.Body);
        Assert.Contains("new.hire@example.com", send.Body);
        Assert.Contains("https://dashboard.skillsetsonline.com/login", send.Body);
    }

    [Fact]
    public async Task EscapesTheNameRatherThanInjectingItAsMarkup()
    {
        var sender = new FakeSender();
        await new AccountWelcomeEmail(sender).SendAsync("x@example.com", "<script>alert(1)</script>", "1234");

        var send = Assert.Single(sender.Sends);
        Assert.DoesNotContain("<script>", send.Body);
        Assert.Contains("&lt;script&gt;", send.Body);
    }

    [Fact]
    public async Task MailFailureNeverThrows()
    {
        // The account already exists by the time this runs - a mail problem must not surface as a
        // failed account creation.
        var email = new AccountWelcomeEmail(new ThrowingSender());
        await email.SendAsync("x@example.com", "Umer", "1234");
    }

    [Fact]
    public async Task MissingFirstName_FallsBackToAGreetingThatStillReads()
    {
        var sender = new FakeSender();
        await new AccountWelcomeEmail(sender).SendAsync("x@example.com", null, "1234");

        Assert.Contains("Hi there", Assert.Single(sender.Sends).Body);
    }

    private sealed record Send(string To, string Subject, string Body, string Purpose);

    private sealed class FakeSender : IEmailSender
    {
        public List<Send> Sends { get; } = [];

        public Task SendAsync(string toAddress, string? toName, string subject, string bodyHtml,
            string? replyToEmail = null, string? replyToName = null, string purpose = "General",
            CancellationToken cancellationToken = default)
        {
            Sends.Add(new Send(toAddress, subject, bodyHtml, purpose));
            return Task.CompletedTask;
        }

        public Task SendToSupportAsync(string subject, string bodyHtml, string? replyToEmail = null,
            string? replyToName = null, string purpose = "General", CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class ThrowingSender : IEmailSender
    {
        public Task SendAsync(string toAddress, string? toName, string subject, string bodyHtml,
            string? replyToEmail = null, string? replyToName = null, string purpose = "General",
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("SMTP is not configured.");

        public Task SendToSupportAsync(string subject, string bodyHtml, string? replyToEmail = null,
            string? replyToName = null, string purpose = "General", CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
