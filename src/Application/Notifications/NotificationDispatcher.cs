using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Notifications;

public record AssignmentNotification(
    string ToEmail,
    string? FirstName,
    string CompanyName,
    IReadOnlyList<string> CourseTitles,
    DateOnly StartDate,
    DateOnly EndDate);

public record ReminderNotification(
    string ToEmail,
    string? FirstName,
    string CompanyName,
    IReadOnlyList<string> CourseTitles,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysRemaining,
    bool HasStarted);

/// <summary>
/// Every automated notification email goes through here, and every one of them checks its
/// SuperAdmin switch (NotificationSettings) before anything is built or sent. Keeping the check in
/// one service rather than at each call site is the point: a new caller cannot forget it, and
/// "toggle off means nothing goes out" stays true without auditing the whole codebase.
///
/// Sending is best-effort throughout - a mail failure must never roll back the assignment, login or
/// import that triggered it. Every send returns whether it actually went out so callers can log it.
/// </summary>
public class NotificationDispatcher
{
    /// <summary>Where an employee actually signs in - the customer-facing portal, never whichever
    /// internal admin host happened to trigger the notification.</summary>
    public const string PortalLoginUrl = "https://dashboard.skillsetsonline.com/login";

    private readonly IEmailSender _emailSender;
    private readonly INotificationSettingsRepository _settingsRepository;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEmailSender emailSender,
        INotificationSettingsRepository settingsRepository,
        ILogger<NotificationDispatcher> logger)
    {
        _emailSender = emailSender;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<bool> SendAssignmentAsync(AssignmentNotification model, CancellationToken cancellationToken = default)
    {
        if (!await IsEnabledAsync(s => s.AssignmentNotificationsEnabled, "assignment", cancellationToken))
        {
            return false;
        }

        var name = FirstNameOrDefault(model.FirstName);
        var titleLine = model.CourseTitles.Count == 1
            ? model.CourseTitles[0]
            : $"{model.CourseTitles.Count} titles";

        var details = new List<EmailLayout.DetailRow>
        {
            new(model.CourseTitles.Count == 1 ? "Course" : "Courses", JoinTitles(model.CourseTitles)),
            new("Starts", model.StartDate.ToString("dddd, MMMM d, yyyy")),
            new("Due by", model.EndDate.ToString("dddd, MMMM d, yyyy")),
            new("Company", model.CompanyName),
            new("Sign in as", model.ToEmail),
        };

        var intro =
            $"You have new training to complete: {EmailLayout.Strong(titleLine)}. " +
            "Sign in to SkillSets to get started - you can work through it at your own pace, and your " +
            "progress is saved as you go.";

        var body = EmailLayout.Render(
            kicker: "New training assigned",
            preheader: $"{titleLine} - due by {model.EndDate:MMM d, yyyy}",
            greeting: $"Hi {name},",
            intro: intro,
            details: details,
            ctaLabel: "Go to SkillSets Dashboard",
            ctaUrl: PortalLoginUrl,
            footerNote:
                $"Please complete this training by <strong>{model.EndDate:MMMM d, yyyy}</strong>. " +
                "These dates were set by whoever assigned the training and do not shift if you start later.");

        return await SendAsync(model.ToEmail, model.FirstName, $"New training assigned: {titleLine}", body, "AssignmentCreated", cancellationToken);
    }

    // SendLoginAsync was removed at the customer's request - the sign-in notification email is gone.
    // Sign-ins are still recorded in LoginActivityLogs and visible under System Logs, which is where
    // access gets reviewed; the email duplicated that while sending one message per employee login.

    public async Task<bool> SendReminderAsync(ReminderNotification model, CancellationToken cancellationToken = default)
    {
        if (!await IsEnabledAsync(s => s.ReminderNotificationsEnabled, "reminder", cancellationToken))
        {
            return false;
        }

        var name = FirstNameOrDefault(model.FirstName);
        var overdue = model.DaysRemaining < 0;

        var window = overdue
            ? $"was due on {model.EndDate:MMMM d, yyyy}"
            : model.DaysRemaining == 0
                ? $"is due today, {model.EndDate:MMMM d, yyyy}"
                : $"is due in {model.DaysRemaining} day{(model.DaysRemaining == 1 ? string.Empty : "s")}, on {model.EndDate:MMMM d, yyyy}";

        var opener = model.HasStarted
            ? "You've made a start on your assigned training - there's still some left to finish."
            : "You haven't started your assigned training yet.";

        var details = new List<EmailLayout.DetailRow>
        {
            new(model.CourseTitles.Count == 1 ? "Course" : "Courses", JoinTitles(model.CourseTitles)),
            new("Started", model.StartDate.ToString("dddd, MMMM d, yyyy")),
            new("Due by", model.EndDate.ToString("dddd, MMMM d, yyyy")),
            new("Company", model.CompanyName),
            new("Sign in as", model.ToEmail),
        };

        var body = EmailLayout.Render(
            kicker: overdue ? "Training overdue" : "Training reminder",
            preheader: $"Your assigned training {window}",
            greeting: $"Hi {name},",
            intro: $"{opener} It {window}.",
            details: details,
            ctaLabel: "Go to SkillSets Dashboard",
            ctaUrl: PortalLoginUrl,
            footerNote: overdue
                ? "Please complete this as soon as you can, or speak to your manager if you need more time."
                : "Already finished it? Then nothing more is needed - well done.");

        var subject = overdue
            ? "Overdue: your SkillSets training"
            : "Reminder: your SkillSets training";

        return await SendAsync(model.ToEmail, model.FirstName, subject, body, "TrainingReminder", cancellationToken);
    }

    /// <summary>An absent settings row means "not configured yet", which must read as ENABLED - these
    /// emails were already going out before the switches existed, so a missing row silently
    /// suppressing them would be a regression rather than a default.</summary>
    private async Task<bool> IsEnabledAsync(
        Func<Domain.Notifications.NotificationSettings, bool> selector,
        string label,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settingsRepository.GetAsync(cancellationToken);
            if (settings is null)
            {
                return true;
            }

            var enabled = selector(settings);
            if (!enabled)
            {
                _logger.LogInformation("Skipping {Notification} notification - turned off in Notification Settings.", label);
            }

            return enabled;
        }
        catch (Exception ex)
        {
            // Fail open, matching the "absent row means enabled" rule above - a settings lookup
            // problem should not quietly stop people being told about their training.
            _logger.LogWarning(ex, "Could not read Notification Settings; sending {Notification} notification anyway.", label);
            return true;
        }
    }

    private async Task<bool> SendAsync(
        string toEmail, string? toName, string subject, string body, string purpose, CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(toEmail, toName, subject, body, purpose: purpose, cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send {Purpose} notification to {Email}", purpose, toEmail);
            return false;
        }
    }

    private static string FirstNameOrDefault(string? firstName) =>
        string.IsNullOrWhiteSpace(firstName) ? "there" : firstName.Trim();

    private static string JoinTitles(IReadOnlyList<string> titles) =>
        titles.Count == 0 ? "Your assigned training" : string.Join(", ", titles);
}
