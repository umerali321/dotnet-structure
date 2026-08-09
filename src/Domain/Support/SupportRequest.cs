using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Support;

/// <summary>A brand-new table backing Settings -> Customer Support.</summary>
public class SupportRequest : IAggregateRoot
{
    public int SupportRequestId { get; private set; }

    public int CompanyId { get; private set; }

    public int UserId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string Status { get; private set; } = SupportRequestStatus.Open;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private SupportRequest()
    {
    }

    public static SupportRequest Create(int companyId, int userId, string subject, string message)
    {
        return new SupportRequest
        {
            CompanyId = companyId,
            UserId = userId,
            Subject = subject,
            Message = message,
            Status = SupportRequestStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateStatus(string status)
    {
        if (!SupportRequestStatus.IsValid(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unrecognized support request status.");
        }

        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
