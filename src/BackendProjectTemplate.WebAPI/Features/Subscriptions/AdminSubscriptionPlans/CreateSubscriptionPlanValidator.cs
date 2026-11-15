using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

public sealed class CreateSubscriptionPlanValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanValidator()
    {
        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(SubscriptionPlan.MaxCodeLength)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Code may contain only letters, digits, underscores and hyphens.");
        RuleFor(request => request.Name).NotEmpty().MaximumLength(SubscriptionPlan.MaxNameLength);
        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0)
            .Must(price => decimal.Truncate(price) == price)
            .WithMessage("Price must be a whole number of minor currency units.");
        RuleFor(request => request.CurrencyId).NotEmpty();
        RuleFor(request => request.BillingInterval)
            .Must(StrictEnumParser.IsValid<BillingInterval>)
            .WithMessage("BillingInterval must be Monthly or Yearly.");
    }
}
