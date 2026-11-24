using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed class PurchaseSubscriptionValidator : AbstractValidator<PurchaseSubscriptionRequest>
{
    public PurchaseSubscriptionValidator()
    {
        RuleFor(request => request.PlanId).NotEmpty();
        RuleFor(request => request.BillingInterval)
            .Must(StrictEnumParser.IsValid<BillingInterval>)
            .WithMessage("BillingInterval must be Monthly or Yearly.");
        RuleFor(request => request.PaymentProviderId).NotEmpty();
        RuleFor(request => request.BillingAccountOwnerType)
            .NotEmpty()
            .Must(value => Enum.TryParse<BillingAccountOwnerType>(value, true, out _))
            .WithMessage("BillingAccountOwnerType must be Tenant or Stakeholder.");
    }
}
