using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_VerifyingBachsPayment_WithNonSuccessfulStatus_Should
{
    [Theory]
    [InlineData("expired", null, PaymentProviderVerificationStatus.Expired)]
    [InlineData("cancelled", null, PaymentProviderVerificationStatus.Failed)]
    [InlineData("open", "failed", PaymentProviderVerificationStatus.Processing)]
    [InlineData("open", "unknown", PaymentProviderVerificationStatus.Processing)]
    public async Task MapStatusSafely(
        string checkoutStatus,
        string? paymentStatus,
        PaymentProviderVerificationStatus expectedStatus)
    {
        var client = Substitute.For<IBachsClient>();
        client.GetCheckoutSessionAsync("chk_123", Arg.Any<CancellationToken>())
            .Returns(new BachsCheckoutSessionResponse(
                "chk_123",
                null,
                checkoutStatus,
                paymentStatus,
                "25.00",
                "NGN",
                "pay_merchant_reference",
                null,
                null));
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

        result.VerificationStatus.ShouldBe(expectedStatus);
    }
}
