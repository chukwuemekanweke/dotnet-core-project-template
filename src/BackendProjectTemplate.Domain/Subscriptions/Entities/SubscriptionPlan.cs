using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions.Entities;

/// <summary>
/// A subscription package (for example Basic, Standard or Premium) priced in one currency. The package offers one
/// price per billing interval through <see cref="Prices"/>, so a monthly and an annual option are the same plan, not
/// separate plans. Offered intervals can be added and repriced but never removed, so existing subscriptions can
/// always resolve the terms of the interval they bought. <see cref="Description"/> and <see cref="Features"/> are
/// display content for plan cards and never affect billing.
/// </summary>
public sealed class SubscriptionPlan : Entity, IAggregateRoot
{
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1000;
    public const int MaxFeatureLength = 200;
    public const int MaxFeatureCount = 20;

    private readonly List<SubscriptionPlanPrice> _prices = [];
    private List<string> _features = [];

    private SubscriptionPlan()
    {
    }

    private SubscriptionPlan(
        string code,
        string name,
        Guid currencyId,
        IReadOnlyDictionary<BillingInterval, decimal> prices,
        bool isActive,
        string? description,
        IReadOnlyList<string>? features)
    {
        ArgumentNullException.ThrowIfNull(prices);
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        _features = NormalizeFeatures(features);
        CurrencyId = currencyId != Guid.Empty
            ? currencyId
            : throw new ArgumentException("Currency id is required.", nameof(currencyId));
        if (prices.Count == 0)
        {
            throw new ArgumentException("A plan must offer at least one billing interval.", nameof(prices));
        }

        foreach (var (billingInterval, price) in prices.OrderBy(item => item.Key))
        {
            _prices.Add(new SubscriptionPlanPrice(Id, billingInterval, price));
        }

        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid CurrencyId { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Ordered, customer-facing feature bullets, for example "Up to 10 team members".</summary>
    public IReadOnlyList<string> Features => _features;
    public IReadOnlyCollection<SubscriptionPlanPrice> Prices => _prices;

    public static SubscriptionPlan Create(
        string code,
        string name,
        Guid currencyId,
        IReadOnlyDictionary<BillingInterval, decimal> prices,
        bool isActive,
        string? description = null,
        IReadOnlyList<string>? features = null) =>
        new(code, name, currencyId, prices, isActive, description, features);

    /// <summary>Creates a plan offered on a single billing interval.</summary>
    public static SubscriptionPlan Create(
        string code,
        string name,
        decimal price,
        Guid currencyId,
        BillingInterval billingInterval,
        bool isActive,
        string? description = null,
        IReadOnlyList<string>? features = null) =>
        new(
            code,
            name,
            currencyId,
            new Dictionary<BillingInterval, decimal> { [billingInterval] = price },
            isActive,
            description,
            features);

    public bool Offers(BillingInterval billingInterval) =>
        _prices.Any(price => price.BillingInterval == billingInterval);

    /// <summary>Returns the price for <paramref name="billingInterval"/>, or throws when the plan does not offer it.</summary>
    public decimal GetPrice(BillingInterval billingInterval) =>
        _prices.SingleOrDefault(price => price.BillingInterval == billingInterval)?.Price
        ?? throw new ArgumentException(
            $"Subscription plan '{Code}' is not offered with a {billingInterval} billing interval.",
            nameof(billingInterval));

    public DateTimeOffset CalculatePeriodEnd(BillingInterval billingInterval, DateTimeOffset periodStartUtc) =>
        Offers(billingInterval)
            ? BillingIntervals.CalculatePeriodEnd(billingInterval, periodStartUtc)
            : throw new ArgumentException(
                $"Subscription plan '{Code}' is not offered with a {billingInterval} billing interval.",
                nameof(billingInterval));

    public void SetActive(bool isActive) => IsActive = isActive;

    /// <summary>
    /// Updates the terms an administrator may change after creation: the name, description, features, the price of
    /// offered intervals, and newly offered intervals. The code and currency are immutable, and an interval cannot
    /// be withdrawn once offered. Historical purchase, period, renewal and allocation snapshots are unaffected.
    /// </summary>
    public bool UpdateTerms(
        string name,
        IReadOnlyDictionary<BillingInterval, decimal> prices,
        string? description,
        IReadOnlyList<string>? features)
    {
        ArgumentNullException.ThrowIfNull(prices);
        var normalizedName = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);
        var normalizedFeatures = NormalizeFeatures(features);
        var withdrawn = _prices.Where(price => !prices.ContainsKey(price.BillingInterval)).ToList();
        if (withdrawn.Count > 0)
        {
            throw new ArgumentException(
                $"Billing interval '{withdrawn[0].BillingInterval}' is already offered and cannot be removed.",
                nameof(prices));
        }

        foreach (var price in prices.Values)
        {
            SubscriptionPlanPrice.ValidatePrice(price);
        }

        var changed = Name != normalizedName ||
            Description != normalizedDescription ||
            !_features.SequenceEqual(normalizedFeatures, StringComparer.Ordinal);
        Name = normalizedName;
        Description = normalizedDescription;
        _features = normalizedFeatures;
        foreach (var (billingInterval, price) in prices.OrderBy(item => item.Key))
        {
            var existing = _prices.SingleOrDefault(item => item.BillingInterval == billingInterval);
            if (existing is null)
            {
                _prices.Add(new SubscriptionPlanPrice(Id, billingInterval, price));
                changed = true;
            }
            else
            {
                changed |= existing.ChangePrice(price);
            }
        }

        return changed;
    }

    public static string NormalizePlanCode(string code) => NormalizeCode(code);

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        return normalized.Length <= MaxDescriptionLength
            ? normalized
            : throw new ArgumentException($"Plan description must not exceed {MaxDescriptionLength} characters.", nameof(description));
    }

    private static List<string> NormalizeFeatures(IReadOnlyList<string>? features)
    {
        var normalized = (features ?? [])
            .Select(feature => feature?.Trim() ?? string.Empty)
            .ToList();
        if (normalized.Count > MaxFeatureCount)
        {
            throw new ArgumentException($"A plan may list at most {MaxFeatureCount} features.", nameof(features));
        }

        if (normalized.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Plan features must not be empty.", nameof(features));
        }

        if (normalized.Any(feature => feature.Length > MaxFeatureLength))
        {
            throw new ArgumentException($"Each plan feature must not exceed {MaxFeatureLength} characters.", nameof(features));
        }

        return normalized;
    }

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
}
