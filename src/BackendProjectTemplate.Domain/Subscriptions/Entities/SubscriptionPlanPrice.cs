using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

/// <summary>
/// The price a <see cref="SubscriptionPlan"/> charges for one billing interval. A plan offers at most one price
/// per interval (for example a monthly and an annual price for the same package). Purchases, periods, renewals and
/// allocations snapshot the price they used, so changing it only affects future purchases.
/// </summary>
public sealed class SubscriptionPlanPrice : Entity
{
    private SubscriptionPlanPrice()
    {
    }

    internal SubscriptionPlanPrice(Guid planId, BillingInterval billingInterval, decimal price)
    {
        PlanId = planId;
        BillingInterval = Enum.IsDefined(billingInterval)
            ? billingInterval
            : throw new ArgumentOutOfRangeException(nameof(billingInterval), billingInterval, "Billing interval is invalid.");
        Price = ValidatePrice(price);
    }

    public Guid PlanId { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public decimal Price { get; private set; }

    internal bool ChangePrice(decimal price)
    {
        var validated = ValidatePrice(price);
        if (Price == validated)
        {
            return false;
        }

        Price = validated;
        return true;
    }

    internal static decimal ValidatePrice(decimal price)
    {
        if (price < 0 || decimal.Truncate(price) != price)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Plan price must be a non-negative whole number of minor currency units.");
        }

        return price;
    }
}
