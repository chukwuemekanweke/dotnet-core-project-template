using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_VerifyingBachsPayment_WithUnderpaidCharge_Should
{
    [Theory]
    [InlineData("expired")]
    [InlineData("cancelled")]
    public async Task ReportUnderpaymentWithCollectedAmount_WhenCheckoutHasClosed(string checkoutStatus)
    {
        var result = await VerifyAsync(checkoutStatus, "underpaid", "10.00", "NGN");

        result.VerificationStatus.ShouldBe(PaymentProviderVerificationStatus.Underpaid);
        result.AmountPaid.ShouldBe(1000m);
        result.ProviderReference.ShouldBe("chk_123");
    }

    [Fact]
    public async Task KeepProcessing_WhenCheckoutIsStillOpen()
    {
        var result = await VerifyAsync("open", "underpaid", "10.00", "NGN");

        result.VerificationStatus.ShouldBe(PaymentProviderVerificationStatus.Processing);
        result.AmountPaid.ShouldBeNull();
    }

    [Theory]
    [InlineData("0.00", "NGN")]
    [InlineData("25.00", "NGN")]
    [InlineData("10.00", "USD")]
    [InlineData("not-a-number", "NGN")]
    public async Task FallBackToExpiry_WhenCollectedAmountIsNotAPartialPayment(string amountPaid, string currency)
    {
        var result = await VerifyAsync("expired", "underpaid", amountPaid, currency);

        result.VerificationStatus.ShouldBe(PaymentProviderVerificationStatus.Expired);
        result.AmountPaid.ShouldBeNull();
    }

    private static Task<PaymentProviderVerificationResult> VerifyAsync(
        string checkoutStatus,
        string chargeStatus,
        string amountPaid,
        string chargeCurrency)
    {
        var client = Substitute.For<IBachsClient>();
        client.GetCheckoutSessionAsync("chk_123", Arg.Any<CancellationToken>())
            .Returns(new BachsCheckoutSessionResponse(
                "chk_123",
                null,
                checkoutStatus,
                null,
                "25.00",
                "NGN",
                "pay_merchant_reference",
                null,
                new BachsCheckoutCharge("ch_123", chargeStatus, "25.00", chargeCurrency, amountPaid)));
        var sut = new BachsPaymentProviderService(
            client,
            Substitute.For<IStakeholderReadModelRepository>(),
            Options.Create(new BachsOptions()));

        return sut.VerifyPaymentAsync(
            new PaymentProviderVerificationRequest(
                "pay_merchant_reference",
                "chk_123",
                2500m,
                "NGN",
                PaymentIntent.WalletTopUp),
            CancellationToken.None);
    }
}
