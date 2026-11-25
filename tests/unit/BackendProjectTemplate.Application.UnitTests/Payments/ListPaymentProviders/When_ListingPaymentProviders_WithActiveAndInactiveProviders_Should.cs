using BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ListPaymentProviders;

public sealed class When_ListingPaymentProviders_WithActiveAndInactiveProviders_Should
{
    [Fact]
    public async Task ReturnEveryProviderWithActivationStateInNameOrder()
    {
        var repository = Substitute.For<IReadRepository<PaymentProvider>>();
        var bachs = PaymentProvider.Create("Bachs", "bachs", true);
        var credo = PaymentProvider.Create("Credo", "credo", true);
        var safeHaven = PaymentProvider.Create("SafeHaven", "safehaven", false);
        ISpecification<PaymentProvider>? captured = null;
        repository.ListAsync(
                Arg.Do<ISpecification<PaymentProvider>>(specification => captured = specification),
                Arg.Any<CancellationToken>())
            .Returns([bachs, credo, safeHaven]);

        var result = await new ListPaymentProvidersHandler(repository).HandleAsync(CancellationToken.None);

        var specification = captured.ShouldBeOfType<PaymentProvidersByNameSpecification>();
        specification.Criteria.ShouldBeNull();
        specification.Includes.ShouldBeEmpty();
        specification.AsNoTracking.ShouldBeTrue();
        specification.OrderBy.ShouldNotBeNull().Compile()(bachs).ShouldBe("Bachs");
        specification.ThenBy.ShouldNotBeNull().Compile()(bachs).ShouldBe(bachs.Id);
        result.ShouldBe([
            new PaymentProviderResponse(bachs.Id, "Bachs", "bachs", true),
            new PaymentProviderResponse(credo.Id, "Credo", "credo", true),
            new PaymentProviderResponse(safeHaven.Id, "SafeHaven", "safehaven", false)
        ]);
    }
}
