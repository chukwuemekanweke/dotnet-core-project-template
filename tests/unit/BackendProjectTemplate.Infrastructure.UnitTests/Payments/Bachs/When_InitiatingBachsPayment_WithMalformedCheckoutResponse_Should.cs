using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_InitiatingBachsPayment_WithMalformedCheckoutResponse_Should
{
    [Fact]
    public async Task RejectInvalidCheckoutUrl()
    {
        var client = Substitute.For<IBachsClient>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();
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
                "not-a-checkout-url",
                "open",
                null,
                null,
                null,
                "pay_merchant_reference",
                null,
                null));
        var sut = new BachsPaymentProviderService(
            client,
            stakeholderRepository,
            Options.Create(new BachsOptions()));

        var action = () => sut.InitiatePaymentAsync(
            new PaymentProviderInitiationRequest(
                "pay_merchant_reference",
                2500m,
                "NGN",
                PaymentIntent.WalletTopUp,
                stakeholderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            CancellationToken.None);

        await action.ShouldThrowAsync<InvalidOperationException>();
    }
}
