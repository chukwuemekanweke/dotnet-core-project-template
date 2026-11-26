using BackendProjectTemplate.Domain.Auditing.Entities;
using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Providers;

public sealed class SetPaymentProviderActivationValidator : AbstractValidator<SetPaymentProviderActivationRequest>
{
    public SetPaymentProviderActivationValidator() =>
        RuleFor(request => request.Reason).MaximumLength(AdministrativeAudit.MaxReasonLength);
}
