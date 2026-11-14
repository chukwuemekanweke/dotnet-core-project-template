using BackendProjectTemplate.Domain.Payments;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed class SubscriptionOwnerValidator : AbstractValidator<SubscriptionOwnerRequest>
{
    public SubscriptionOwnerValidator()
    {
        RuleFor(request => request.BillingAccountOwnerType)
            .NotEmpty()
            .Must(value => Enum.TryParse<BillingAccountOwnerType>(value, true, out _))
            .WithMessage("BillingAccountOwnerType must be Tenant or Stakeholder.");
    }
}
