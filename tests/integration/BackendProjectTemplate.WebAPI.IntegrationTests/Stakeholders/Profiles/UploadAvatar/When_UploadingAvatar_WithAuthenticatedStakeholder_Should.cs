using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Storage;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Stakeholders.Profiles;

[Collection(nameof(ContainersCollection))]
public sealed class When_UploadingAvatar_WithAuthenticatedStakeholder_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _providerId;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateVerifiedUserAsync();
        await SeedFileStorageProviderAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task StoreAvatarUrl()
    {
        UploadAvatarResponse? payload = default;

        await WhenUploadingAvatar();
        await ThenTheAvatarUrlIsPersisted();

        async Task WhenUploadingAvatar()
        {
            using var content = new MultipartFormDataContent();
            using var avatarContent = new ByteArrayContent([1, 2, 3, 4]);
            avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(avatarContent, "avatar", "avatar.png");

            _response = await Client.PostAsync($"{EndpointUrl.Stakeholders.V1}/me/profile/avatar", content);
            payload = await _response.Content.ReadFromJsonAsync<UploadAvatarResponse>();
        }

        async Task ThenTheAvatarUrlIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            payload.ShouldNotBeNull();
            payload.AvatarUrl.ShouldContain("example.invalid");

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
            var stakeholder = await repository.GetByIdAsync(_stakeholderId);

            stakeholder.ShouldNotBeNull();
            stakeholder.AvatarUrl.ShouldBe(payload.AvatarUrl);
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var firstName = WebApiIntegrationTestData.FirstName();
        var lastName = WebApiIntegrationTestData.LastName();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, firstName, lastName, now);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified(now);
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer", now);
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, _countryId, stakeholderType.Id, firstName, lastName, now);

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task SeedFileStorageProviderAsync()
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var provider = Provider.Create(ProviderType.FileStorage, "Noop", ObjectStorageProviderKeys.Noop, true, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow());

        _providerId = provider.Id;
        await repository.AddAsync(provider);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var writeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var existing = await readRepository.ListAsync(new FirstCountrySpecification());
        if (existing.Count > 0)
        {
            return existing[0].Id;
        }

        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg", scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow());
        await writeRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;

        return country.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var provider = await providerRepository.GetByIdAsync(_providerId);
        if (provider is not null)
        {
            providerRepository.Remove(provider);
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(_stakeholderId);
        if (stakeholder is not null)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        var stakeholderType = await stakeholderTypeRepository.GetByIdAsync(_stakeholderTypeId);
        if (stakeholderType is not null)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
