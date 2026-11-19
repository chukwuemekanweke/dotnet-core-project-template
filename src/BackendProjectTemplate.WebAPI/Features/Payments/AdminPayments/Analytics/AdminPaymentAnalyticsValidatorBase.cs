using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments.Analytics;

public abstract class AdminPaymentAnalyticsValidatorBase<T> : AbstractValidator<T>
    where T : AdminPaymentAnalyticsRequest
{
    protected AdminPaymentAnalyticsValidatorBase(PaymentAnalyticsOptions options, TimeProvider timeProvider)
    {
        RuleFor(request => request.TenantId).Must(BeNullOrNonEmpty).WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.PaymentProviderId).Must(BeNullOrNonEmpty).WithMessage("PaymentProviderId must not be an empty GUID.");
        RuleFor(request => request.CurrencyId).Must(BeNullOrNonEmpty).WithMessage("CurrencyId must not be an empty GUID.");
        RuleFor(request => request.PaymentIntent)
            .Must(StrictEnumParser.IsValidOrEmpty<PaymentIntent>)
            .WithMessage("PaymentIntent must be WalletTopUp or Subscription.");
        RuleFor(request => request.PaymentStatus)
            .Must(StrictEnumParser.IsValidOrEmpty<PaymentStatus>)
            .WithMessage("PaymentStatus is invalid.");
        RuleFor(request => request)
            .Must(request => IsValidRange(request, options, timeProvider))
            .WithMessage($"The inclusive reporting range must be valid, ordered, and no longer than {options.MaximumRangeDays} days.");
    }

    private static bool IsValidRange(AdminPaymentAnalyticsRequest request, PaymentAnalyticsOptions options, TimeProvider timeProvider)
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

    private static bool BeNullOrNonEmpty(Guid? value) => !value.HasValue || value.Value != Guid.Empty;
}
