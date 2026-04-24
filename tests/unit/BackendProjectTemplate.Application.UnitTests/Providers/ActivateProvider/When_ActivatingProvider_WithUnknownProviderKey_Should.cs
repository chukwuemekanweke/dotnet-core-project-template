using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Providers.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Providers.ActivateProvider;

public sealed class When_ActivatingProvider_WithUnknownProviderKey_Should
{
    [Fact]
    public async Task ReturnProviderNotFound()
    {
        var providerRepository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = TimeProvider.System;

        providerRepository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([
                Provider.Create(ProviderType.Email, "Primary", "primary", true, timeProvider.GetUtcNow())
            ]);

        var sut = new ActivateProviderHandler(providerRepository, unitOfWork, timeProvider);

        var result = await sut.HandleAsync(
            new ActivateProviderCommand(ProviderType.Email, "missing"),
            CancellationToken.None);

        result.Status.ShouldBe(ActivateProviderStatus.ProviderNotFound);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
