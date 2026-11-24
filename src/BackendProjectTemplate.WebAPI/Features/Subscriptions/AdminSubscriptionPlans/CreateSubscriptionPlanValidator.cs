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
        RuleFor(request => request.Prices)
            .NotNull()
            .WithMessage("At least one billing interval price is required.")
            .SetValidator(new SubscriptionPlanPricesValidator());
        RuleFor(request => request.CurrencyId).NotEmpty();
        RuleFor(request => request.Description).MaximumLength(SubscriptionPlan.MaxDescriptionLength);
        RuleFor(request => request.Features)
            .Must(features => features is null || features.Count <= SubscriptionPlan.MaxFeatureCount)
            .WithMessage($"A plan may list at most {SubscriptionPlan.MaxFeatureCount} features.");
        RuleForEach(request => request.Features)
            .NotEmpty()
            .MaximumLength(SubscriptionPlan.MaxFeatureLength);
    }
}
