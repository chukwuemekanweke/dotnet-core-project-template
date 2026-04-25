using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using BackendProjectTemplate.Application.UnitTests.Payments;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ProcessCredoWebhook;

public sealed class When_ProcessingCredoWebhook_WithMissingEventName_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var paymentProviderService = Substitute.For<IPaymentProviderService>();

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Payments.Entities.PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        paymentProviderService.ValidateWebhookAsync(Arg.Any<PaymentProviderWebhookValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Valid, null));
        context.PaymentProviderServices.Add(paymentProviderService);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateCredoWebhookHandler().HandleAsync(
                new ProcessCredoWebhookCommand(
                    new CredoWebhook(
                        " ",
                        new CredoWebhookData(
                            "700607002190001",
                            "trans-ref",
                            "merchant-ref",
                            1000m,
                            1000m,
                            15m,
                            985m,
                            "customer@example.com",
                            "May 7, 2023, 1:37:53 AM",
                            1,
                            "NGN",
                            0,
                            "MasterCard",
                            "Card",
                            new CredoWebhookCustomer("customer@example.com", "John", "Doe", "23470122199999"))),
                    "{}"),
                CancellationToken.None));

        exception.Message.ShouldContain("required");
    }
}
