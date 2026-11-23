using BackendProjectTemplate.Domain.Payments;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

public sealed class BillingHistoryValidator : AbstractValidator<BillingHistoryRequest>
{
    public BillingHistoryValidator()
    {
        RuleFor(request => request.BillingAccountOwnerType)
            .NotEmpty()
            .Must(value => Enum.TryParse<BillingAccountOwnerType>(value, true, out _))
            .WithMessage("BillingAccountOwnerType must be Tenant or Stakeholder.");
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
