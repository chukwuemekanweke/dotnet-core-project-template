using BackendProjectTemplate.Domain.Subscriptions.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

public sealed class UpdateSubscriptionPlanValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(SubscriptionPlan.MaxNameLength);
        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0)
            .Must(price => decimal.Truncate(price) == price)
            .WithMessage("Price must be a whole number of minor currency units.");
    }
}
