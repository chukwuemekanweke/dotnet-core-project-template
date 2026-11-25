using BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;
using BackendProjectTemplate.Domain.Payments.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ListPaymentProviders;

public sealed class When_ListingPaymentProviders_WithNoProviders_Should
{
    [Fact]
    public async Task ReturnEmptyCollection()
    {
        var repository = Substitute.For<IReadRepository<PaymentProvider>>();
        repository.ListAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PaymentProvider>());

        var result = await new ListPaymentProvidersHandler(repository).HandleAsync(CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
