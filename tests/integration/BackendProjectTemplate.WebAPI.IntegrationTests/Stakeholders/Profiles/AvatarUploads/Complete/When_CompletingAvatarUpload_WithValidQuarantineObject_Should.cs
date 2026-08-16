using BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Stakeholders.Profiles.AvatarUploads.Complete;

[Collection(nameof(ContainersCollection))]
public sealed class When_CompletingAvatarUpload_WithValidQuarantineObject_Should(ContainersFixture fixture)
    : AvatarUploadIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PromoteAvatarAndExposeItFromProfile()
    {
        using var createResponse = await Client.PostAsJsonAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/profile/avatar/uploads",
            new CreateAvatarUploadRequest("avatar.png", "image/png", 12));
        var createPayload = await createResponse.Content.ReadFromJsonAsync<CreateAvatarUploadResponse>();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        createPayload.ShouldNotBeNull();
        TrackUpload(createPayload.UploadId);

        using (var scope = CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<AvatarUpload>>();
            var upload = await repository.GetByIdAsync(createPayload.UploadId);
            upload.ShouldNotBeNull();
            ObjectStorageService.StorePrivateObject(
                upload.QuarantineObjectKey,
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 },
                "image/png");
        }

        using var completeResponse = await Client.PostAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/profile/avatar/uploads/{createPayload.UploadId}/complete",
            null);
        var completePayload = await completeResponse.Content.ReadFromJsonAsync<CompleteAvatarUploadResponse>();
        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        completePayload.ShouldNotBeNull();

        using var profileResponse = await Client.GetAsync($"{EndpointUrl.Stakeholders.V1}/me/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetProfileResponse>();
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        profile.ShouldNotBeNull();
        profile.AvatarUrl.ShouldBe(completePayload.AvatarUrl);
        profile.AvatarUrl.ShouldNotBeNull();
        profile.AvatarUrl.ShouldContain($"/avatar/{createPayload.UploadId:N}.png");
    }
}
