using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Providers.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Providers.ActivateProvider;

public sealed class When_ActivatingProvider_WithMatchingProviderKey_Should
{
    [Fact]
    public async Task MarkSelectedProviderActive()
    {
        var providerRepository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var transaction = Substitute.For<IUnitOfWorkTransaction>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);
        var auditRecorder = Substitute.For<IAdministrativeAuditRecorder>();
        var activeProvider = Provider.Create(ProviderType.Email, "Primary", "primary", true);
        var inactiveProvider = Provider.Create(ProviderType.Email, "Secondary", "secondary", false);

        providerRepository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([activeProvider, inactiveProvider]);

        var sut = new ActivateProviderHandler(providerRepository, auditRecorder, unitOfWork);

        var result = await sut.HandleAsync(
            new ActivateProviderCommand(ProviderType.Email, "secondary", new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ActivateProviderStatus.Success);
        activeProvider.IsActive.ShouldBeFalse();
        inactiveProvider.IsActive.ShouldBeTrue();
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await auditRecorder.Received(1).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry =>
                entry.Action == AdministrativeAuditActions.Providers.Deactivated && entry.TargetId == activeProvider.Id),
            Arg.Any<CancellationToken>());
        await auditRecorder.Received(1).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry =>
                entry.Action == AdministrativeAuditActions.Providers.Activated && entry.TargetId == inactiveProvider.Id),
            Arg.Any<CancellationToken>());
    }

}


