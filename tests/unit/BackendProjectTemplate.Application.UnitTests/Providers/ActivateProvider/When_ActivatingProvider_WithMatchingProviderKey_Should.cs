using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Providers.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Providers.ActivateProvider;

public sealed class When_ActivatingProvider_WithMatchingProviderKey_Should
{
    [Fact]
    public async Task MarkSelectedProviderActive()
    {
        var providerRepository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var createdAt = new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
        var activeProvider = Provider.Create(ProviderType.Email, "Primary", "primary", true, createdAt);
        var inactiveProvider = Provider.Create(ProviderType.Email, "Secondary", "secondary", false, createdAt);

        providerRepository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([activeProvider, inactiveProvider]);

        var sut = new ActivateProviderHandler(providerRepository, unitOfWork);

        var result = await sut.HandleAsync(
            new ActivateProviderCommand(ProviderType.Email, "secondary"),
            CancellationToken.None);

        result.Status.ShouldBe(ActivateProviderStatus.Success);
        activeProvider.IsActive.ShouldBeFalse();
        inactiveProvider.IsActive.ShouldBeTrue();
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

}
