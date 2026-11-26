using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Providers.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Providers.ActivateProvider;

public sealed class When_ActivatingProvider_WithUnknownProviderKey_Should
{
    [Fact]
    public async Task ReturnProviderNotFound()
    {
        var providerRepository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        providerRepository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([
                Provider.Create(ProviderType.Email, "Primary", "primary", true)
            ]);

        var auditRecorder = Substitute.For<IAdministrativeAuditRecorder>();
        var sut = new ActivateProviderHandler(providerRepository, auditRecorder, unitOfWork);

        var result = await sut.HandleAsync(
            new ActivateProviderCommand(ProviderType.Email, "missing", new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ActivateProviderStatus.ProviderNotFound);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await auditRecorder.DidNotReceiveWithAnyArgs().RecordAsync(default!, default);
    }
}


