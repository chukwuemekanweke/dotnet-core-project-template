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

public sealed class When_UpdatingProfile_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnNoContent()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Initial", "User", DateTimeOffset.UtcNow);
        var request = new UpdateProfileRequest("Jane", "Doe");

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);

        var sut = new ProfilesController(
            new UploadAvatarHandler(stakeholderRepository, Substitute.For<IObjectStorageService>(), customTelemetryContext, unitOfWork, TimeProvider.System),
            new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork, TimeProvider.System),
            currentActor);

        var result = await sut.UpdateProfile(request, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
    }
}
