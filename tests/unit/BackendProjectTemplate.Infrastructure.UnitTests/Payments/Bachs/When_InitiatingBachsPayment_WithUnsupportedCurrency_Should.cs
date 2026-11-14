using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_InitiatingBachsPayment_WithUnsupportedCurrency_Should
{
    [Fact]
    public async Task RejectBeforeCallingProvider()
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
        var sut = new BachsPaymentProviderService(
            client,
            stakeholderRepository,
            Options.Create(new BachsOptions()));

        var action = () => sut.InitiatePaymentAsync(
            new PaymentProviderInitiationRequest(
                "pay_merchant_reference",
                2500m,
                "EUR",
                PaymentIntent.WalletTopUp,
                stakeholderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            CancellationToken.None);

        await action.ShouldThrowAsync<InvalidOperationException>();
        await client.DidNotReceiveWithAnyArgs().CreateCheckoutSessionAsync(default!, default!, default);
    }
}
