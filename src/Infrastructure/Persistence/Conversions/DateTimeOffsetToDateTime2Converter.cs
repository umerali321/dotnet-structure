using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SkillsetsBackend.Infrastructure.Persistence.Conversions;

/// <summary>
/// Bridges DateTimeOffset (used throughout the domain/API for consistency) to the existing
/// database's datetime2 audit columns, which carry no offset. Those columns are populated via
/// sysutcdatetime(), so stored values are always treated as UTC when read back.
/// </summary>
public class DateTimeOffsetToDateTime2Converter : ValueConverter<DateTimeOffset, DateTime>
{
    public static readonly DateTimeOffsetToDateTime2Converter Instance = new();

    public DateTimeOffsetToDateTime2Converter()
        : base(
            toDb => toDb.UtcDateTime,
            fromDb => new DateTimeOffset(DateTime.SpecifyKind(fromDb, DateTimeKind.Utc)))
    {
    }
}

public class NullableDateTimeOffsetToDateTime2Converter : ValueConverter<DateTimeOffset?, DateTime?>
{
    public static readonly NullableDateTimeOffsetToDateTime2Converter Instance = new();

    public NullableDateTimeOffsetToDateTime2Converter()
        : base(
            toDb => toDb.HasValue ? toDb.Value.UtcDateTime : null,
            fromDb => fromDb.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(fromDb.Value, DateTimeKind.Utc)) : null)
    {
    }
}
