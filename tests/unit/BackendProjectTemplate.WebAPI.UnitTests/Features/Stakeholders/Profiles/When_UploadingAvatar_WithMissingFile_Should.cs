using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_UploadingAvatar_WithMissingFile_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var sut = new ProfilesController(
            new UploadAvatarHandler(
                currentActor,
                Substitute.For<IRepository<Stakeholder>>(),
                Substitute.For<IObjectStorageService>(),
                Substitute.For<IUnitOfWork>(),
                TimeProvider.System),
            new UpdateProfileHandler(
                currentActor,
                Substitute.For<IRepository<Stakeholder>>(),
                Substitute.For<IUnitOfWork>(),
                TimeProvider.System));

        var result = await sut.UploadAvatar(new UploadAvatarRequest(null!), CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
