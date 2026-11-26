using BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;
using BackendProjectTemplate.Domain.Auditing;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ActivatePaymentProvider;

public sealed class When_ActivatingPaymentProvider_WithExistingProvider_Should
{
    [Fact]
    public async Task SetActivation()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Credo", "credo", false);

        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(provider);

        var result = await context.CreateActivatePaymentProviderHandler().HandleAsync(
            new ActivatePaymentProviderCommand(provider.Id, true, context.Actor, "Primary provider restored"),
            CancellationToken.None);

        result.Status.ShouldBe(ActivatePaymentProviderStatus.Success);
        provider.IsActive.ShouldBeTrue();
        context.PaymentProviderRepository.Received(1).Update(provider);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.AuditRecorder.Received(1).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry =>
                entry.Action == AdministrativeAuditActions.PaymentProviders.Activated &&
                entry.TargetType == AdministrativeAuditTargetTypes.PaymentProvider &&
                entry.TargetId == provider.Id &&
                entry.TargetTenantId == null &&
                entry.Reason == "Primary provider restored" &&
                entry.Changes!.ToJson() == "{\"isActive\":{\"previous\":false,\"current\":true}}"),
            Arg.Any<CancellationToken>());
    }
}
