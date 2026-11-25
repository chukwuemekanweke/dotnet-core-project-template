using BackendProjectTemplate.WebAPI.Features.Payments.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Payments.Providers.SetActivation;

public sealed class When_SettingPaymentProviderActivation_WithUnknownProvider_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new PaymentProvidersController(
            context.CreateActivatePaymentProviderHandler(),
            context.CreateListPaymentProvidersHandler());

        var result = await sut.SetActivation(Guid.CreateVersion7(), new SetPaymentProviderActivationRequest(true), CancellationToken.None);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
