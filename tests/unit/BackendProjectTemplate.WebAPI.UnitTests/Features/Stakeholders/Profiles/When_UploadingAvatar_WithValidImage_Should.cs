using System.Text;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_UploadingAvatar_WithValidImage_Should
{
    [Fact]
    public async Task ReturnAvatarUrl()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", DateTimeOffset.UtcNow);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("avatar"));
        var avatar = new FormFile(stream, 0, stream.Length, "avatar", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
        objectStorageService.UploadPublicAsync(Arg.Any<ObjectStorageUploadRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://example.com/avatar.png");

        var sut = new ProfilesController(
            new UploadAvatarHandler(currentActor, stakeholderRepository, objectStorageService, customTelemetryContext, unitOfWork, TimeProvider.System),
            new UpdateProfileHandler(currentActor, stakeholderRepository, customTelemetryContext, unitOfWork, TimeProvider.System));

        var result = await sut.UploadAvatar(new UploadAvatarRequest(avatar), CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<UploadAvatarResponse>();
        payload.AvatarUrl.ShouldBe("https://example.com/avatar.png");
    }
}
