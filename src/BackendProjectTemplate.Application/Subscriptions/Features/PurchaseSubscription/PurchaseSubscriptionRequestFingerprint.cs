using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;

public static class PurchaseSubscriptionRequestFingerprint
{
    public static string Compute(PurchaseSubscriptionCommand command)
    {
        var canonical = string.Join(
            '|',
            command.PlanId.ToString("N"),
            command.PaymentProviderId.ToString("N"),
            ((int)command.BillingAccountOwnerType).ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.AutoRenew ? "1" : "0");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
