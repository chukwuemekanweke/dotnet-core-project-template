using BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.WebAPI.Features.Payments.Providers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Payments.Providers.List;

public sealed class When_ListingPaymentProviders_WithProviders_Should
{
    [Fact]
    public async Task ReturnOkWithProviderSummaries()
    {
        var context = new PaymentsControllerTestContext();
        var active = context.CreatePaymentProvider("Credo", "credo");
        var inactive = context.CreatePaymentProvider("SafeHaven", "safehaven", isActive: false);
        context.PaymentProviderReadRepository
            .ListAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns([active, inactive]);
        var sut = new PaymentProvidersController(
            context.CreateActivatePaymentProviderHandler(),
            context.CreateListPaymentProvidersHandler());

        var result = await sut.List(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(new[]
        {
            new PaymentProviderResponse(active.Id, "Credo", "credo", true),
            new PaymentProviderResponse(inactive.Id, "SafeHaven", "safehaven", false)
        });
    }
}
