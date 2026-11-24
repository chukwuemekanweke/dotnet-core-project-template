using BackendProjectTemplate.Domain.Subscriptions;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;

/// <summary>
/// Canonical SHA-256 fingerprint of every caller-supplied allocation input (tenant, owner, stakeholder, plan,
/// requested window as sent, source, reason and billing interval when given). Timestamps are normalized to UTC microseconds and the reason
/// is trimmed, so a faithful retry matches while any material change does not.
/// </summary>
public static class AllocateSubscriptionRequestFingerprint
{
    private const string Version = "v1";

    public static string Compute(AllocateSubscriptionCommand command)
    {
        var canonical = string.Join(
            '|',
            Version,
            command.TenantId.ToString("N"),
            ((int)command.BillingAccountOwnerType).ToString(CultureInfo.InvariantCulture),
            command.StakeholderId?.ToString("N") ?? "-",
            command.PlanId.ToString("N"),
            Format(command.StartAtUtc),
            Format(command.EndAtUtc),
            ((int)command.AllocationSource).ToString(CultureInfo.InvariantCulture),
            command.Reason?.Trim() ?? string.Empty);

        // Appended only when supplied, so fingerprints of requests made before intervals existed stay stable.
        if (command.BillingInterval.HasValue)
        {
            canonical += "|" + ((int)command.BillingInterval.Value).ToString(CultureInfo.InvariantCulture);
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Format(DateTimeOffset? value) =>
        value.HasValue
            ? SubscriptionTimestamps.Normalize(value.Value).UtcTicks.ToString(CultureInfo.InvariantCulture)
            : "-";
}
