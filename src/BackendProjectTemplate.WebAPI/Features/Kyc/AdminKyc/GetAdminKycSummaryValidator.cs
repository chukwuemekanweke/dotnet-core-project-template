using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Kyc.AdminKyc;

public sealed class GetAdminKycSummaryValidator : AbstractValidator<GetAdminKycSummaryRequest>
{
    public GetAdminKycSummaryValidator()
    {
        RuleFor(request => request.TenantId)
            .Must(value => !value.HasValue || value.Value != Guid.Empty)
            .WithMessage("TenantId must not be an empty GUID.");
        RuleFor(request => request.ProviderKey)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 50)
            .WithMessage("ProviderKey must not exceed 50 characters.");
    }
}
