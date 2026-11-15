using BackendProjectTemplate.Domain.Subscriptions.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed class AdminSubscriptionCancellationValidator : AbstractValidator<AdminSubscriptionCancellationRequest>
{
    public AdminSubscriptionCancellationValidator()
    {
        RuleFor(request => request.TenantId).NotEmpty();
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(Subscription.MaxCancellationReasonLength);
    }
}
