using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_InitiatingBachsPayment_WithValidRequest_Should
{
    [Fact]
    public async Task CreateIdempotentCheckoutInMajorUnits()
    {
        var client = Substitute.For<IBachsClient>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();
        var request = new PaymentProviderInitiationRequest(
            "pay_merchant_reference",
            2500m,
            "NGN",
            PaymentIntent.WalletTopUp,
            stakeholderId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                Guid.CreateVersion7(),
                "ada@example.com",
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Ada",
                "Lovelace",
                null,
                true));
        client.CreateCheckoutSessionAsync(
                Arg.Any<BachsCreateCheckoutSessionRequest>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new BachsCheckoutSessionResponse(
                "chk_123",
                "https://checkout.bachs.io/c/checkout-token",
                "open",
                null,
                null,
                null,
                request.MerchantReference,
                new DateTimeOffset(2026, 11, 14, 13, 0, 0, TimeSpan.Zero),
                null));
        var sut = new BachsPaymentProviderService(
            client,
            stakeholderRepository,
            Options.Create(new BachsOptions()));

        var result = await sut.InitiatePaymentAsync(request, CancellationToken.None);

        result.ProviderReference.ShouldBe("chk_123");
        result.PaymentMethodType.ShouldBe(PaymentMethodType.PaymentLink);
        result.InstructionFields["paymentLink"].ShouldBe("https://checkout.bachs.io/c/checkout-token");
        await client.Received(1).CreateCheckoutSessionAsync(
            Arg.Is<BachsCreateCheckoutSessionRequest>(payload =>
                payload.Pricing.Amount == "25.00" &&
                payload.Pricing.Currency == "NGN" &&
                payload.Reference == request.MerchantReference &&
                payload.Customer.Email == "ada@example.com"),
            request.MerchantReference,
            Arg.Any<CancellationToken>());
    }
}
