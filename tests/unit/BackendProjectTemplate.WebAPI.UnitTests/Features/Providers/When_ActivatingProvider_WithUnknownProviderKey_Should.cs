using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Providers.Entities;
using BackendProjectTemplate.WebAPI.Features.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Providers;

public sealed class When_ActivatingProvider_WithUnknownProviderKey_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var repository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var request = new ActivateProviderRequest("Email", "missing");

        repository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([
                Provider.Create(ProviderType.Email, "Primary", "primary", true)
            ]);

        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());
        currentActor.CorrelationId.Returns("correlation");
        var sut = new ProvidersController(
            new ActivateProviderHandler(repository, Substitute.For<IAdministrativeAuditRecorder>(), unitOfWork),
            new ActivateProviderValidator(),
            currentActor);

        var result = await sut.ActivateProvider(request, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<NotFoundObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}


