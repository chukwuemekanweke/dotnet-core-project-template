using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Stakeholders.Profiles.AvatarUploads.Create;

[Collection(nameof(ContainersCollection))]
public sealed class When_CreatingAvatarUpload_WithAuthenticatedStakeholder_Should(ContainersFixture fixture)
    : AvatarUploadIntegrationTestBase(fixture)
{
    [Fact]
    public async Task ReturnPrivateUploadInstructionsAndPersistPendingUpload()
    {
        using var response = await Client.PostAsJsonAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/profile/avatar/uploads",
            new CreateAvatarUploadRequest("profile.exe", "image/jpeg", 128));
        var payload = await response.Content.ReadFromJsonAsync<CreateAvatarUploadResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Method.ShouldBe("PUT");
        payload.Headers["Content-Type"].ShouldBe("image/jpeg");
        payload.UploadUrl.ShouldStartWith(StorageServer.Urls.Single());
        payload.UploadUrl.ShouldContain($"/{PrivateBucketName}/{ApplicationFolder}/quarantine/avatars/");
        TrackUpload(payload.UploadId);
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<AvatarUpload>>();
        var upload = await repository.GetByIdAsync(payload.UploadId);
        upload.ShouldNotBeNull();
        upload.Status.ShouldBe(AvatarUploadStatus.Pending);
        upload.FileExtension.ShouldBe(".jpg");
        upload.QuarantineObjectKey.ShouldNotContain("profile.exe");
    }
}
