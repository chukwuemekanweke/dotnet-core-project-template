using BackendProjectTemplate.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class PaymentWebhookInboxConfiguration : IEntityTypeConfiguration<PaymentWebhookInbox>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ValueComparer<Dictionary<string, string>> DictionaryComparer = new(
        (left, right) => Serialize(left) == Serialize(right),
        value => Serialize(value).GetHashCode(),
        value => value.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal));

    public void Configure(EntityTypeBuilder<PaymentWebhookInbox> builder)
    {
        builder.ToTable("PaymentWebhookInboxes", SchemaNames.Payments);

        builder.HasKey(inbox => inbox.Id);

        builder.Property(inbox => inbox.MerchantReference)
            .HasMaxLength(100);

        builder.Property(inbox => inbox.ProviderReference)
            .HasMaxLength(100);

        builder.Property(inbox => inbox.WebhookEventName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(inbox => inbox.WebhookEventId)
            .HasMaxLength(200);

        builder.Property(inbox => inbox.RawPayload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(inbox => inbox.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value))
            .Metadata.SetValueComparer(DictionaryComparer);

        builder.Property(inbox => inbox.StatusChangeReason)
            .HasMaxLength(500);

        builder.Property(inbox => inbox.ProcessingError)
            .HasMaxLength(4000);

        builder.Property(inbox => inbox.RowVersion)
            .IsRowVersion();

        builder.HasIndex(inbox => inbox.MerchantReference)
            .HasFilter("[MerchantReference] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(inbox => inbox.ProviderReference)
            .HasFilter("[ProviderReference] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(inbox => inbox.WebhookProcessingStatus);

        builder.HasIndex(inbox => new { inbox.PaymentProviderId, inbox.WebhookEventId })
            .IsUnique()
            .HasFilter("[WebhookEventId] IS NOT NULL AND [IsDeleted] = 0");
    }

    private static string Serialize(Dictionary<string, string>? value) =>
        JsonSerializer.Serialize(value ?? [], JsonSerializerOptions);

    private static Dictionary<string, string> Deserialize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, string>>(value, JsonSerializerOptions) ?? [];
}
