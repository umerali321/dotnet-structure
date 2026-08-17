namespace SkillsetsBackend.Application.Common.Exceptions;

public class AuthenticationFailedException : Exception
{
    /// <summary>How many failed login attempts this email has accrued in the current lockout
    /// window, including this one - null when not applicable (e.g. SuperAdmin path, or callers
    /// that don't track attempts). Lets the frontend warn the user before they get locked out.</summary>
    public int? FailedAttemptCount { get; }

    public int? RemainingAttempts { get; }

    public AuthenticationFailedException(string message, int? failedAttemptCount = null, int? remainingAttempts = null)
        : base(message)
    {
        FailedAttemptCount = failedAttemptCount;
        RemainingAttempts = remainingAttempts;
    }
}
