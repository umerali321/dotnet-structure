using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Notifications;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("NotificationSettings");
        builder.HasKey(x => x.NotificationSettingsId);

        builder.Property(x => x.ReminderNotificationsEnabled).IsRequired();
        builder.Property(x => x.LoginNotificationsEnabled).IsRequired();
        builder.Property(x => x.AssignmentNotificationsEnabled).IsRequired();

        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.UpdatedAt).HasConversion(NullableDateTimeOffsetToDateTime2Converter.Instance);
    }
}
