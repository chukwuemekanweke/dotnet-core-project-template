using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_InitiatingBachsSubscriptionWithAutoRenew_Should
{
    [Fact]
    public async Task CreateCustomerAndRequestSavedPaymentMethod()
    {
        var client = Substitute.For<IBachsClient>();
        var stakeholders = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();
        var request = new PaymentProviderInitiationRequest(
            "pay_subscription",
            125000m,
            "NGN",
            PaymentIntent.Subscription,
            stakeholderId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            true,
            true);
        stakeholders.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(
            new StakeholderReadModel(
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
        client.CreateCustomerAsync(Arg.Any<BachsCreateCustomerRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BachsCustomerResponse("cust_123", "ada@example.com", "Ada Lovelace"));
        client.CreateCheckoutSessionAsync(Arg.Any<BachsCreateCheckoutSessionRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BachsCheckoutSessionResponse(
                "chk_123",
                "https://checkout.bachs.io/c/token",
                "open",
                null,
                null,
                null,
                request.MerchantReference,
                DateTimeOffset.UtcNow.AddHours(1),
                null));
        var sut = new BachsPaymentProviderService(client, stakeholders, Options.Create(new BachsOptions()));

        var result = await sut.InitiatePaymentAsync(request, CancellationToken.None);

        result.RecurringPaymentSetup.ShouldNotBeNull();
        result.RecurringPaymentSetup!.ProviderCustomerReference.ShouldBe("cust_123");
        await client.Received(1).CreateCheckoutSessionAsync(
            Arg.Is<BachsCreateCheckoutSessionRequest>(payload =>
                payload.SavePaymentMethod &&
                payload.Customer.CustomerId == "cust_123" &&
                payload.Customer.Email == null),
            request.MerchantReference,
            Arg.Any<CancellationToken>());
    }
}
