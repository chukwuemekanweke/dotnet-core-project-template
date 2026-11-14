using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_VerifyingBachsPayment_WithSucceededCheckout_Should
{
    [Fact]
    public async Task ReturnSucceededOnlyAfterIdentityChecks()
    {
        var client = Substitute.For<IBachsClient>();
        client.GetCheckoutSessionAsync("chk_123", Arg.Any<CancellationToken>())
            .Returns(new BachsCheckoutSessionResponse(
                "chk_123",
                null,
                "completed",
                "succeeded",
                "25.00",
                "NGN",
                "pay_merchant_reference",
                null,
                new BachsCheckoutCharge("pay_123", "succeeded", "25.00", "NGN")));
        var sut = new BachsPaymentProviderService(
            client,
            Substitute.For<IStakeholderReadModelRepository>(),
            Options.Create(new BachsOptions()));

        var result = await sut.VerifyPaymentAsync(
            new PaymentProviderVerificationRequest(
                "pay_merchant_reference",
                "chk_123",
                2500m,
                "NGN",
                PaymentIntent.WalletTopUp),
            CancellationToken.None);

        result.VerificationStatus.ShouldBe(PaymentProviderVerificationStatus.Succeeded);
        result.ProviderReference.ShouldBe("chk_123");
    }
}
