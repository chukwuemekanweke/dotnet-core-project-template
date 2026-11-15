using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

public sealed class ListSubscriptionsValidator : AbstractValidator<ListSubscriptionsRequest>
{
    public ListSubscriptionsValidator()
    {
        RuleFor(request => request.Limit).InclusiveBetween(1, 100);
        RuleFor(request => request.BillingAccountOwnerType)
            .Must(StrictEnumParser.IsValidOrEmpty<BillingAccountOwnerType>)
            .WithMessage("BillingAccountOwnerType must be Tenant or Stakeholder.");
        RuleFor(request => request.Status)
            .Must(StrictEnumParser.IsValidOrEmpty<SubscriptionStatus>)
            .WithMessage("Status must be Pending, Scheduled, Active, Cancelled or Expired.");
        RuleFor(request => request.AllocationSource)
            .Must(StrictEnumParser.IsValidOrEmpty<SubscriptionAllocationSource>)
            .WithMessage("AllocationSource must be AdminAllocation, Promotion, Migration or Trial.");
        RuleFor(request => request.CreatedToUtc)
            .GreaterThan(request => request.CreatedFromUtc)
            .When(request => request.CreatedFromUtc.HasValue && request.CreatedToUtc.HasValue);
        RuleFor(request => request.CurrentPeriodToUtc)
            .GreaterThan(request => request.CurrentPeriodFromUtc)
            .When(request => request.CurrentPeriodFromUtc.HasValue && request.CurrentPeriodToUtc.HasValue);
        RuleFor(request => request.Cursor)
            .Must(BeDecodable)
            .When(request => !string.IsNullOrWhiteSpace(request.Cursor))
            .WithMessage("Cursor is invalid.");
    }

    private static bool BeDecodable(string? cursor)
    {
        try
        {
            CursorPagination.Decode(cursor);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
