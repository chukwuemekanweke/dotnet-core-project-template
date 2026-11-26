using BackendProjectTemplate.Domain.Auditing.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

public sealed class SetSubscriptionPlanActivationValidator : AbstractValidator<SetSubscriptionPlanActivationRequest>
{
    public SetSubscriptionPlanActivationValidator() =>
        RuleFor(request => request.Reason).MaximumLength(AdministrativeAudit.MaxReasonLength);
}
