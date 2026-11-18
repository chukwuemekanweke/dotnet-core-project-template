using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments;

public sealed class ListAdminPaymentsValidator : AbstractValidator<ListAdminPaymentsRequest>
{
    public ListAdminPaymentsValidator()
    {
        RuleFor(request => request.Limit).InclusiveBetween(1, 100);
        RuleFor(request => request.TenantId).Must(BeNullOrNonEmpty).WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.StakeholderId).Must(BeNullOrNonEmpty).WithMessage("StakeholderId must not be an empty GUID.");
        RuleFor(request => request.PaymentProviderId).Must(BeNullOrNonEmpty).WithMessage("PaymentProviderId must not be an empty GUID.");
        RuleFor(request => request.CurrencyId).Must(BeNullOrNonEmpty).WithMessage("CurrencyId must not be an empty GUID.");
        RuleFor(request => request.PaymentIntent)
            .Must(StrictEnumParser.IsValidOrEmpty<PaymentIntent>)
            .WithMessage("PaymentIntent must be WalletTopUp or Subscription.");
        RuleFor(request => request.PaymentStatus)
            .Must(StrictEnumParser.IsValidOrEmpty<PaymentStatus>)
            .WithMessage("PaymentStatus is invalid.");
        RuleFor(request => request.PaymentMethodType)
            .Must(StrictEnumParser.IsValidOrEmpty<PaymentMethodType>)
            .WithMessage("PaymentMethodType must be PaymentLink, BankTransfer or Card.");
        RuleFor(request => request.CreatedToUtc)
            .GreaterThan(request => request.CreatedFromUtc)
            .When(request => request.CreatedFromUtc.HasValue && request.CreatedToUtc.HasValue);
        RuleFor(request => request.Search)
            .Must(search => string.IsNullOrWhiteSpace(search) || search.Trim().Length is >= 2 and <= 100)
            .WithMessage("Search must contain between 2 and 100 characters.");
        RuleFor(request => request.Cursor)
            .Must(BeDecodable)
            .When(request => !string.IsNullOrWhiteSpace(request.Cursor))
            .WithMessage("Cursor is invalid.");
    }

    private static bool BeNullOrNonEmpty(Guid? value) => !value.HasValue || value.Value != Guid.Empty;

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
