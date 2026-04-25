using System.Text.Json;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Credo;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Payments.Webhooks.Credo;

public sealed class When_ReceivingCredoWebhook_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnOk()
    {
        var context = new PaymentsControllerTestContext();
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var providerService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create(
            "merchant-ref",
            PaymentIntent.WalletTopUp,
            provider.Id,
            1000m,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            context.Clock.GetUtcNow());

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentWebhookInbox>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.PaymentTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        providerService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        providerService.ValidateWebhookAsync(Arg.Any<PaymentProviderWebhookValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Valid, null));
        context.PaymentProviderServices.Add(providerService);

        var payload = new
        {
            businessCode = "700607002190001",
            transRef = "trans-ref",
            businessRef = "merchant-ref",
            debitedAmount = 1000m,
            transAmount = 1000m,
            transFeeAmount = 15m,
            settlementAmount = 985m,
            customerId = "customer@example.com",
            transactionDate = "May 7, 2023, 1:37:53 AM",
            channelId = 1,
            currencyCode = "NGN",
            status = 0,
            paymentMethodType = "MasterCard",
            paymentMethod = "Card",
            customer = new
            {
                customerEmail = "customer@example.com",
                firstName = "John",
                lastName = "Doe",
                phoneNo = "23470122199999"
            }
        };
        var request = new CredoWebhookRequest(
            CredoWebhookEvents.TransactionSuccessful,
            JsonSerializer.SerializeToElement(payload));
        var sut = new CredoWebhooksController(context.CreateCredoWebhookHandler());

        var result = await sut.Handle(request, CancellationToken.None);

        result.ShouldBeOfType<OkResult>();
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
