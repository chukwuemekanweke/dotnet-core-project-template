using FluentValidation;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Providers;

public sealed class SetPaymentProviderActivationValidator : AbstractValidator<SetPaymentProviderActivationRequest>
{
    public SetPaymentProviderActivationValidator()
    {
    }
}
