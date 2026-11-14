using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.InitiatePayment;

public sealed class When_InitiatingGenericSubscriptionPayment_Should
{
    [Fact]
    public async Task RejectClientSuppliedPricing()
    {
        var context = new PaymentsFlowTestContext();
        var command = new InitiatePaymentCommand(
            1m,
            Guid.CreateVersion7(),
            PaymentIntent.Subscription,
            Guid.CreateVersion7(),
            new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow"));

        var action = () => context.CreateInitiatePaymentHandler().HandleAsync(command, CancellationToken.None);

        await action.ShouldThrowAsync<InvalidOperationException>();
    }
}
