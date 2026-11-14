using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_ChargingBachsSavedCard_Should
{
    [Theory]
    [InlineData("NGN", 250000, "2500.00")]
    [InlineData("USD", 2599, "25.99")]
    public async Task UseSavedReferencesAndStableIdempotencyKey(string currency, decimal amount, string majorAmount)
    {
        var client = Substitute.For<IBachsClient>();
        var merchantReference = $"pay_{Guid.CreateVersion7():N}";
        client.CreateChargeAsync(Arg.Any<BachsCreateChargeRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new BachsChargeResponse(
                "ch_123",
                "succeeded",
                majorAmount,
                currency,
                merchantReference,
                null,
                null));
        var sut = new BachsPaymentProviderService(
            client,
            Substitute.For<IStakeholderReadModelRepository>(),
            Options.Create(new BachsOptions()));

        var result = await sut.ChargeAsync(
            new RecurringChargeRequest(
                merchantReference,
                amount,
                currency,
                "cust_123",
                "pm_123",
                "renewal-key",
                new Dictionary<string, string>()),
            CancellationToken.None);

        result.Status.ShouldBe(RecurringChargeStatus.Succeeded);
        await client.Received(1).CreateChargeAsync(
            Arg.Is<BachsCreateChargeRequest>(payload =>
                payload.Customer == "cust_123" &&
                payload.PaymentMethod == "pm_123" &&
                payload.Amount == majorAmount &&
                payload.Reference == merchantReference),
            "renewal-key",
            Arg.Any<CancellationToken>());
    }
}
