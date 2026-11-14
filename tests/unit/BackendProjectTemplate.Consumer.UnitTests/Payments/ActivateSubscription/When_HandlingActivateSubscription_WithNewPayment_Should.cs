using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithNewPayment_Should
{
    [Fact]
    public async Task CreateActivation()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateActivateSubscriptionCommand(5000m, Guid.CreateVersion7());
        SubscriptionActivation? capturedActivation = null;

        context.SubscriptionActivationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionActivation>>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionActivation?)null);
        context.SubscriptionActivationRepository.AddAsync(Arg.Do<SubscriptionActivation>(activation => capturedActivation = activation), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        capturedActivation.ShouldNotBeNull();
        capturedActivation.PaymentTransactionId.ShouldBe(command.PaymentTransactionId);
        capturedActivation.BillingAccountId.ShouldNotBe(Guid.Empty);
        capturedActivation.Amount.ShouldBe(command.Amount);
        capturedActivation.SubscriptionId.ShouldNotBeNull();
        capturedActivation.SubscriptionPeriodId.ShouldNotBeNull();
        await context.SubscriptionLifecyclePersistence.Received(1).AddPeriodAsync(
            Arg.Any<SubscriptionPeriod>(),
            Arg.Any<CancellationToken>());
        await context.SubscriptionLifecyclePersistence.Received(1).AddStatusHistoryAsync(
            Arg.Any<SubscriptionStatusHistory>(),
            Arg.Any<CancellationToken>());
        await context.CommandSender.Received(1).ScheduleSendAsync(
            Arg.Is<RenewSubscriptionCommand>(renewal => renewal.SubscriptionId == capturedActivation.SubscriptionId),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
