using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_UpdatingProfile_WithMissingNames_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        var sut = new ProfilesController(
            new UploadAvatarHandler(
                currentActor,
                Substitute.For<IRepository<Stakeholder>>(),
                Substitute.For<IObjectStorageService>(),
                customTelemetryContext,
                Substitute.For<IUnitOfWork>(),
                TimeProvider.System),
            new UpdateProfileHandler(
                currentActor,
                Substitute.For<IRepository<Stakeholder>>(),
                customTelemetryContext,
                Substitute.For<IUnitOfWork>(),
                TimeProvider.System));

        var result = await sut.UpdateProfile(new UpdateProfileRequest(string.Empty, string.Empty), CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
