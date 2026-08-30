using Microsoft.Extensions.Logging.Abstractions;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Notifications;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Notifications;

namespace SkillsetsBackend.UnitTests.Notifications;

/// <summary>The promise the Notification Service screen makes is "switch it off and it does not
/// go out" - so these assert that nothing reaches the mail layer at all when a switch is off, not
/// merely that the call returned false.</summary>
public class NotificationDispatcherTests
{
    private static readonly AssignmentNotification Assignment = new(
        "employee@example.com", "Alanna", "Acme Corp", ["Harassment Prevention"],
        new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1));

    private static readonly ReminderNotification Reminder = new(
        "employee@example.com", "Alanna", "Acme Corp", ["Harassment Prevention"],
        new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), DaysRemaining: 3, HasStarted: false);

    private static readonly LoginNotification Login = new(
        "employee@example.com", "Alanna", new DateTimeOffset(2026, 8, 30, 9, 0, 0, TimeSpan.Zero));

    private static NotificationDispatcher Build(FakeSender sender, NotificationSettings? settings) =>
        new(sender, new FakeSettingsRepository(settings), NullLogger<NotificationDispatcher>.Instance);

    private static NotificationSettings SettingsWith(bool reminder, bool login, bool assignment)
    {
        var settings = NotificationSettings.CreateDefault();
        settings.Update(reminder, login, assignment);
        return settings;
    }

    [Fact]
    public async Task AssignmentSwitchOff_SendsNothing()
    {
        var sender = new FakeSender();
        var sent = await Build(sender, SettingsWith(reminder: true, login: true, assignment: false)).SendAssignmentAsync(Assignment);

        Assert.False(sent);
        Assert.Empty(sender.Sends);
    }

    [Fact]
    public async Task ReminderSwitchOff_SendsNothing()
    {
        var sender = new FakeSender();
        var sent = await Build(sender, SettingsWith(reminder: false, login: true, assignment: true)).SendReminderAsync(Reminder);

        Assert.False(sent);
        Assert.Empty(sender.Sends);
    }

    [Fact]
    public async Task LoginSwitchOff_SendsNothing()
    {
        var sender = new FakeSender();
        var sent = await Build(sender, SettingsWith(reminder: true, login: false, assignment: true)).SendLoginAsync(Login);

        Assert.False(sent);
        Assert.Empty(sender.Sends);
    }

    [Fact]
    public async Task OneSwitchOff_DoesNotSilenceTheOthers()
    {
        var sender = new FakeSender();
        var dispatcher = Build(sender, SettingsWith(reminder: false, login: true, assignment: true));

        Assert.False(await dispatcher.SendReminderAsync(Reminder));
        Assert.True(await dispatcher.SendLoginAsync(Login));
        Assert.True(await dispatcher.SendAssignmentAsync(Assignment));

        Assert.Equal(2, sender.Sends.Count);
    }

    [Fact]
    public async Task NoSettingsRowYet_StillSends()
    {
        // These emails were already going out before the switches existed - an absent row must read
        // as "enabled", or adding the feature would silently stop them.
        var sender = new FakeSender();
        var sent = await Build(sender, settings: null).SendAssignmentAsync(Assignment);

        Assert.True(sent);
        Assert.Single(sender.Sends);
    }

    [Fact]
    public async Task SettingsLookupFails_StillSends()
    {
        // Fail open: a settings problem should not quietly stop people being told about their training.
        var sender = new FakeSender();
        var dispatcher = new NotificationDispatcher(
            sender, new ThrowingSettingsRepository(), NullLogger<NotificationDispatcher>.Instance);

        Assert.True(await dispatcher.SendAssignmentAsync(Assignment));
        Assert.Single(sender.Sends);
    }

    [Fact]
    public async Task MailFailure_IsReportedButNeverThrows()
    {
        // The caller has already created the assignment/account by this point - a bounced email must
        // not surface as a failed operation.
        var dispatcher = new NotificationDispatcher(
            new ThrowingSender(), new FakeSettingsRepository(null), NullLogger<NotificationDispatcher>.Instance);

        Assert.False(await dispatcher.SendAssignmentAsync(Assignment));
    }

    [Fact]
    public async Task AssignmentEmail_CarriesTheDatesCourseAndRecipient()
    {
        var sender = new FakeSender();
        await Build(sender, settings: null).SendAssignmentAsync(Assignment);

        var send = Assert.Single(sender.Sends);
        Assert.Equal("employee@example.com", send.To);
        Assert.Equal("AssignmentCreated", send.Purpose);
        Assert.Contains("Harassment Prevention", send.Body);
        Assert.Contains("Acme Corp", send.Body);
        Assert.Contains("October 1, 2026", send.Body);
        Assert.Contains(NotificationDispatcher.PortalLoginUrl, send.Body);
    }

    [Fact]
    public async Task OverdueReminder_ReadsAsOverdueRatherThanDueInNegativeDays()
    {
        var sender = new FakeSender();
        var overdue = Reminder with { DaysRemaining = -4 };
        await Build(sender, settings: null).SendReminderAsync(overdue);

        var send = Assert.Single(sender.Sends);
        Assert.Contains("Overdue", send.Subject);
        Assert.DoesNotContain("-4", send.Body);
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

    private sealed class FakeSettingsRepository(NotificationSettings? settings) : INotificationSettingsRepository
    {
        public Task<NotificationSettings?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(settings);
        public void Add(NotificationSettings s) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingSettingsRepository : INotificationSettingsRepository
    {
        public Task<NotificationSettings?> GetAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Database unavailable.");
        public void Add(NotificationSettings s) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
