using BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;
using BackendProjectTemplate.Application.UnitTests.Payments;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ActivatePaymentProvider;

public sealed class When_ActivatingPaymentProvider_WithUnknownProvider_Should
{
    [Fact]
    public async Task ReturnProviderNotFound()
    {
        var context = new PaymentsFlowTestContext();
        var paymentProviderId = Guid.CreateVersion7();

        var result = await context.CreateActivatePaymentProviderHandler().HandleAsync(
            new ActivatePaymentProviderCommand(paymentProviderId, true),
            CancellationToken.None);

        result.Status.ShouldBe(ActivatePaymentProviderStatus.ProviderNotFound);
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
