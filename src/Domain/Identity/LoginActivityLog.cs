using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>Event type constants for LoginActivityLog.EventType - kept as plain strings (not an
/// enum-backed column) so new event types are additive and don't need a migration.</summary>
public static class LoginActivityEventTypes
{
    public const string PasswordResetSucceeded = "PasswordResetSucceeded";
    public const string PasswordResetFailed = "PasswordResetFailed";
    public const string SupportRequestSubmitted = "SupportRequestSubmitted";
}

/// <summary>Brand-new table (not a legacy one) - audit trail for the login page's self-service
/// "forgot password" and "contact support" flows, so SuperAdmin can see how many resets
/// succeeded/failed and who reached out to support.</summary>
public class LoginActivityLog : IAggregateRoot
{
    public int LoginActivityLogId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    /// <summary>Populated only for PasswordResetSucceeded - the account that was reset.</summary>
    public int? UserId { get; private set; }

    /// <summary>Populated only for SupportRequestSubmitted.</summary>
    public string? Name { get; private set; }

    public string? Phone { get; private set; }

    public string? CompanyName { get; private set; }

    public string? Message { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private LoginActivityLog()
    {
    }

    public static LoginActivityLog PasswordResetSucceeded(string email, int userId) => new()
    {
        EventType = LoginActivityEventTypes.PasswordResetSucceeded,
        Email = email,
        UserId = userId,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public static LoginActivityLog PasswordResetFailed(string email) => new()
    {
        EventType = LoginActivityEventTypes.PasswordResetFailed,
        Email = email,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public static LoginActivityLog SupportRequestSubmitted(string name, string email, string? phone, string? companyName, string? message) => new()
    {
        EventType = LoginActivityEventTypes.SupportRequestSubmitted,
        Name = name,
        Email = email,
        Phone = phone,
        CompanyName = companyName,
        Message = message,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
