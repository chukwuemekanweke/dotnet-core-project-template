using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;

public sealed class ListAdminKycVerificationsValidator : AbstractValidator<ListAdminKycVerificationsRequest>
{
    public ListAdminKycVerificationsValidator()
    {
        RuleFor(request => request.Limit).InclusiveBetween(1, 100);
        RuleFor(request => request.TenantId)
            .Must(BeNullOrNonEmpty)
            .WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.StakeholderId)
            .Must(BeNullOrNonEmpty)
            .WithMessage("StakeholderId must not be an empty GUID.");
        RuleFor(request => request.Status)
            .Must(StrictEnumParser.IsValidOrEmpty<KycVerificationStatus>)
            .WithMessage("Status is invalid.");
        RuleFor(request => request.ProviderKey)
            .Must(value => HasValidLength(value, 50))
            .WithMessage("ProviderKey must contain between 1 and 50 characters.");
        RuleFor(request => request.ProviderStatus)
            .Must(value => HasValidLength(value, 50))
            .WithMessage("ProviderStatus must contain between 1 and 50 characters.");
        RuleFor(request => request.ProviderReference)
            .Must(value => HasValidLength(value, 100))
            .WithMessage("ProviderReference must contain between 1 and 100 characters.");
        RuleFor(request => request.Search)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length is >= 2 and <= 100)
            .WithMessage("Search must contain between 2 and 100 characters.");
        RuleFor(request => request.StartedToUtc)
            .GreaterThan(request => request.StartedFromUtc)
            .When(request => request.StartedFromUtc.HasValue && request.StartedToUtc.HasValue);
        RuleFor(request => request.Cursor)
            .Must(BeDecodable)
            .When(request => !string.IsNullOrWhiteSpace(request.Cursor))
            .WithMessage("Cursor is invalid.");
    }

    private static bool BeNullOrNonEmpty(Guid? value) => !value.HasValue || value.Value != Guid.Empty;

    private static bool HasValidLength(string? value, int maximum) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maximum;

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
