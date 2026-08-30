using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Notifications;

/// <summary>Singleton row (there is only ever at most one) holding the SuperAdmin-managed master
/// switches for the automated notification emails. A switch turned off here stops that notification
/// from being sent at all - it is checked before the message is built, so nothing reaches SMTP and
/// nothing is written to Email History.
///
/// Deliberately scoped to the automated/bulk notifications only. Transactional mail a person is
/// waiting on - a password reset, a support request, the credentials for an account just created -
/// is never gated by these, since silently dropping those would leave someone unable to sign in
/// with no indication why.</summary>
public class NotificationSettings : IAggregateRoot
{
    public int NotificationSettingsId { get; private set; }

    /// <summary>Course/training reminder emails driven by an assignment's own start and end dates.</summary>
    public bool ReminderNotificationsEnabled { get; private set; }

    /// <summary>The "a new sign-in to your account" email sent to an employee after they log in.</summary>
    public bool LoginNotificationsEnabled { get; private set; }

    /// <summary>The "new training has been assigned to you" email sent when an assignment is created.</summary>
    public bool AssignmentNotificationsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private NotificationSettings()
    {
    }

    /// <summary>Defaults every switch ON - this record only ever comes into existence the first time
    /// a SuperAdmin saves the screen, and until then the system already sends these emails, so
    /// creating it must not silently change behaviour.</summary>
    public static NotificationSettings CreateDefault()
    {
        return new NotificationSettings
        {
            ReminderNotificationsEnabled = true,
            LoginNotificationsEnabled = true,
            AssignmentNotificationsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(bool reminderEnabled, bool loginEnabled, bool assignmentEnabled)
    {
        ReminderNotificationsEnabled = reminderEnabled;
        LoginNotificationsEnabled = loginEnabled;
        AssignmentNotificationsEnabled = assignmentEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
