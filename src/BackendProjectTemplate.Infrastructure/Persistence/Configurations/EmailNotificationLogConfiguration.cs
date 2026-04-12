using BackendProjectTemplate.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class EmailNotificationLogConfiguration : IEntityTypeConfiguration<EmailNotificationLog>
{
    public void Configure(EntityTypeBuilder<EmailNotificationLog> builder)
    {
        builder.ToTable("EmailNotificationLogs", SchemaNames.Notifications);

        builder.HasKey(log => log.Id);

        builder.Property(log => log.MessageId)
            .IsRequired();

        builder.Property(log => log.To)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(log => log.Cc)
            .HasMaxLength(4000);

        builder.Property(log => log.Bcc)
            .HasMaxLength(4000);

        builder.Property(log => log.IsSent)
            .IsRequired();

        builder.Property(log => log.FailureReason)
            .HasMaxLength(4000);

        builder.HasIndex(log => log.MessageId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
