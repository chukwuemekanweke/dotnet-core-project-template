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
using Microsoft.Extensions.Logging.Abstractions;
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
            new GetProfileHandler(Substitute.For<IStakeholderReadModelRepository>()),
            new UploadAvatarHandler(
                Substitute.For<IRepository<Stakeholder>>(),
                Substitute.For<IObjectStorageService>(),
                customTelemetryContext,
                Substitute.For<IUnitOfWork>()),
            new UpdateProfileHandler(
                Substitute.For<IRepository<Stakeholder>>(),
                customTelemetryContext,
                Substitute.For<IUnitOfWork>()),
            currentActor,
            NullLogger<ProfilesController>.Instance);

        var result = await sut.UpdateProfile(new UpdateProfileRequest(string.Empty, string.Empty), CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
