using BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_GettingProfile_WithoutAuthenticatedStakeholder_Should
{
    [Fact]
    public async Task ReturnUnauthorized()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns("anonymous");
        currentActor.TenantId.Returns(Guid.CreateVersion7());
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var logger = Substitute.For<ILogger<ProfilesController>>();
        var sut = CreateController(stakeholderReadModelRepository, currentActor, logger);

        var result = await sut.GetProfile(CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedResult>();
        var errorWasLogged = logger.ReceivedCalls().Any(call =>
        {
            var arguments = call.GetArguments();
            return call.GetMethodInfo().Name == nameof(ILogger.Log) &&
                Equals(arguments[0], LogLevel.Error) &&
                arguments[2]?.ToString()?.Contains("anonymous", StringComparison.Ordinal) == true;
        });
        errorWasLogged.ShouldBeTrue();
        await stakeholderReadModelRepository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderIdAsync(default, default);
    }

    private static ProfilesController CreateController(
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        ICurrentActor currentActor,
        ILogger<ProfilesController> logger)
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        return new ProfilesController(
            new GetProfileHandler(stakeholderReadModelRepository),
            new UploadAvatarHandler(
                stakeholderRepository,
                Substitute.For<IObjectStorageService>(),
                customTelemetryContext,
                unitOfWork),
            new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork),
            currentActor,
            logger);
    }
}
