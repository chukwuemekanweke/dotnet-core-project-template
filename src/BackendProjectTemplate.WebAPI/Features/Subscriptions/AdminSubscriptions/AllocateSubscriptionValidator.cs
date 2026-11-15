using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed class AllocateSubscriptionValidator : AbstractValidator<AllocateSubscriptionRequest>
{
    public AllocateSubscriptionValidator()
    {
        RuleFor(request => request.TenantId).NotEmpty();
        RuleFor(request => request.PlanId).NotEmpty();
        RuleFor(request => request.IdempotencyKey).NotEmpty();
        RuleFor(request => request.BillingAccountOwnerType)
            .Must(StrictEnumParser.IsValid<BillingAccountOwnerType>)
            .WithMessage("BillingAccountOwnerType must be Tenant or Stakeholder.");
        RuleFor(request => request.StakeholderId)
            .NotEmpty()
            .When(request => StrictEnumParser.ParseOptional<BillingAccountOwnerType>(request.BillingAccountOwnerType) == BillingAccountOwnerType.Stakeholder)
            .WithMessage("StakeholderId is required for a stakeholder-owned billing account.");
        RuleFor(request => request.StakeholderId)
            .Null()
            .When(request => StrictEnumParser.ParseOptional<BillingAccountOwnerType>(request.BillingAccountOwnerType) == BillingAccountOwnerType.Tenant)
            .WithMessage("StakeholderId must be omitted for a tenant-owned billing account.");
        RuleFor(request => request.AllocationSource)
            .Must(StrictEnumParser.IsValid<SubscriptionAllocationSource>)
            .WithMessage("AllocationSource must be AdminAllocation, Promotion, Migration or Trial.");
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(SubscriptionAllocation.MaxReasonLength);
        RuleFor(request => request.AutoRenew)
            .Must(autoRenew => autoRenew != true)
            .WithMessage("Administrative allocations never renew automatically; AutoRenew must be false or omitted.");
        RuleFor(request => request.EndAtUtc)
            .GreaterThan(request => request.StartAtUtc)
            .When(request => request.StartAtUtc.HasValue && request.EndAtUtc.HasValue)
            .WithMessage("EndAtUtc must be later than StartAtUtc.");
    }
}
