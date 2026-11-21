using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Payments;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;

public abstract class AdminSubscriptionAnalyticsValidatorBase<T> : AbstractValidator<T>
    where T : AdminSubscriptionAnalyticsRequest
{
    protected AdminSubscriptionAnalyticsValidatorBase(
        SubscriptionAnalyticsOptions options,
        TimeProvider timeProvider)
    {
        RuleFor(request => request.PlanId).Must(IsNullOrNonEmpty).WithMessage("PlanId must not be an empty GUID.");
        RuleFor(request => request.TenantId).Must(IsNullOrNonEmpty).WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.CurrencyId).Must(IsNullOrNonEmpty).WithMessage("CurrencyId must not be an empty GUID.");
        RuleFor(request => request.OwnerType)
            .Must(StrictEnumParser.IsValidOrEmpty<BillingAccountOwnerType>)
            .WithMessage("OwnerType must be Tenant or Stakeholder.");
        RuleFor(request => request)
            .Must(request => IsValidRange(request, options, timeProvider))
            .WithMessage($"The inclusive reporting range must be valid, ordered, and no longer than {options.MaximumRangeDays} days.");
    }

    private static bool IsValidRange(
        AdminSubscriptionAnalyticsRequest request,
        SubscriptionAnalyticsOptions options,
        TimeProvider timeProvider)
    {
        var to = request.To ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = request.From ?? SafeAddDays(to, -(options.DefaultRangeDays - 1));
        return from.HasValue && from.Value <= to && to != DateOnly.MaxValue &&
            to.DayNumber - from.Value.DayNumber + 1 <= options.MaximumRangeDays;
    }

    private static DateOnly? SafeAddDays(DateOnly value, int days)
    {
        try
        {
            return value.AddDays(days);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static bool IsNullOrNonEmpty(Guid? value) => !value.HasValue || value.Value != Guid.Empty;
}
