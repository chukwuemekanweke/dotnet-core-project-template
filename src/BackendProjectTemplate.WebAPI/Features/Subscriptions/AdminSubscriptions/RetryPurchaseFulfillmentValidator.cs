using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed class RetryPurchaseFulfillmentValidator : AbstractValidator<RetryPurchaseFulfillmentRequest>
{
    public RetryPurchaseFulfillmentValidator() => RuleFor(request => request.TenantId).NotEmpty();
}
