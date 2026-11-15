using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

public sealed class SubscriptionPlan : Entity, IAggregateRoot
{
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 200;

    private SubscriptionPlan()
    {
    }

    private SubscriptionPlan(
        string code,
        string name,
        decimal price,
        Guid currencyId,
        BillingInterval billingInterval,
        bool isActive)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        Price = ValidatePrice(price);
        CurrencyId = currencyId != Guid.Empty
            ? currencyId
            : throw new ArgumentException("Currency id is required.", nameof(currencyId));
        BillingInterval = Enum.IsDefined(billingInterval)
            ? billingInterval
            : throw new ArgumentOutOfRangeException(nameof(billingInterval), billingInterval, "Billing interval is invalid.");
        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Guid CurrencyId { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public bool IsActive { get; private set; }

    public static SubscriptionPlan Create(
        string code,
        string name,
        decimal price,
        Guid currencyId,
        BillingInterval billingInterval,
        bool isActive) =>
        new(code, name, price, currencyId, billingInterval, isActive);

    public DateTimeOffset CalculatePeriodEnd(DateTimeOffset periodStartUtc) =>
        BillingIntervals.CalculatePeriodEnd(BillingInterval, periodStartUtc);

    public void SetActive(bool isActive) => IsActive = isActive;

    public void ChangePrice(decimal price) => Price = ValidatePrice(price);

    /// <summary>
    /// Updates the commercial terms an administrator may change after creation. The code, currency and
    /// billing interval are immutable; historical purchase, period and renewal snapshots are unaffected.
    /// </summary>
    public bool UpdateTerms(string name, decimal price)
    {
        var normalizedName = NormalizeName(name);
        var validatedPrice = ValidatePrice(price);
        if (Name == normalizedName && Price == validatedPrice)
        {
            return false;
        }

        Name = normalizedName;
        Price = validatedPrice;
        return true;
    }

    public static string NormalizePlanCode(string code) => NormalizeCode(code);

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Plan code is required.", nameof(code));
        }

        if (normalized.Length > MaxCodeLength)
        {
            throw new ArgumentException($"Plan code must not exceed {MaxCodeLength} characters.", nameof(code));
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Plan name is required.", nameof(name));
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException($"Plan name must not exceed {MaxNameLength} characters.", nameof(name));
        }

        return normalized;
    }

    private static decimal ValidatePrice(decimal price)
    {
        if (price < 0 || decimal.Truncate(price) != price)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Plan price must be a non-negative whole number of minor currency units.");
        }

        return price;
    }
}
