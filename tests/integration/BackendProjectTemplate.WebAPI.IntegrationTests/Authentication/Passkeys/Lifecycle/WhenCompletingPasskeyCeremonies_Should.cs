using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using StackExchange.Redis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCompletingPasskeyCeremonies_Should : WebApiIntegrationTestBase, IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private readonly List<HttpResponseMessage> _responses = [];
    private readonly byte[] _credentialId = [11, 22, 33, 44];
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private bool _createdCountry;
    private AppUser _user = default!;
    private UserPasskeyInfo _passkey = default!;
    private MockPasskeyState _passkeyState = default!;

    public WhenCompletingPasskeyCeremonies_Should(ContainersFixture fixture)
        : base(fixture, configureServices: services =>
        {
            services.RemoveAll<IPasskeyHandler<AppUser>>();
            services.AddSingleton<MockPasskeyState>();
            services.AddScoped<IPasskeyHandler<AppUser>, MockPasskeyHandler>();
        })
    {
    }

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedUserAsync();
        ConfigurePasskeyHandler();
    }

    public async Task DisposeAsync()
    {
        foreach (var response in _responses)
        {
            response.Dispose();
        }

        await DeleteTestDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task PersistCredentialConsumeChallengesAndIssueSession()
    {
        var initialSignIn = await SignInAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", initialSignIn.AccessToken);

        var registrationOptions = await CreateRegistrationOptionsAsync();
        await AssertRedisKeyExistsAsync($"authentication:passkey-registration:{registrationOptions.FlowToken}");

        var registrationResponse = Track(await Client.PostAsJsonAsync(
            EndpointUrl.Passkeys.RegistrationsV1,
            new CompletePasskeyRequest(registrationOptions.FlowToken, JsonSerializer.Deserialize<JsonElement>("{}"))));
        registrationResponse.StatusCode.ShouldBe(HttpStatusCode.Created,
            await registrationResponse.Content.ReadAsStringAsync());
        (await GetPersistedPasskeysAsync()).Single().CredentialId.ShouldBe(_credentialId);

        var replayedRegistration = Track(await Client.PostAsJsonAsync(
            EndpointUrl.Passkeys.RegistrationsV1,
            new CompletePasskeyRequest(registrationOptions.FlowToken, JsonSerializer.Deserialize<JsonElement>("{}"))));
        replayedRegistration.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await AssertRedisKeyMissingAsync($"authentication:passkey-registration:{registrationOptions.FlowToken}");
        await AssertRedisKeyExistsAsync($"authentication:passkey-registration-marker:{registrationOptions.FlowToken}");

        Client.DefaultRequestHeaders.Authorization = null;
        var sessionsBeforePasskeySignIn = await GetSessionCountAsync();
        var authenticationOptions = await CreateAuthenticationOptionsAsync();
        await AssertRedisKeyExistsAsync($"authentication:passkey-sign-in:{authenticationOptions.FlowToken}");

        var completionRequest = new CompletePasskeyRequest(
            authenticationOptions.FlowToken,
            JsonSerializer.Deserialize<JsonElement>("{}"));
        var authenticationResponses = await Task.WhenAll(
            Client.PostAsJsonAsync(EndpointUrl.Passkeys.AuthenticationCompletionsV1, completionRequest),
            Client.PostAsJsonAsync(EndpointUrl.Passkeys.AuthenticationCompletionsV1, completionRequest));
        foreach (var response in authenticationResponses)
        {
            Track(response);
        }

        authenticationResponses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        authenticationResponses.Count(response => response.StatusCode != HttpStatusCode.OK).ShouldBe(1);
        var authenticationResponse = authenticationResponses.Single(response => response.StatusCode == HttpStatusCode.OK);
        authenticationResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            await authenticationResponse.Content.ReadAsStringAsync());
        var signIn = await authenticationResponse.Content.ReadFromJsonAsync<PasskeySignInResponse>();
        string.IsNullOrWhiteSpace(signIn?.AccessToken).ShouldBeFalse();
        (await GetSessionCountAsync()).ShouldBe(sessionsBeforePasskeySignIn + 1);

        var replayedAuthentication = Track(await Client.PostAsJsonAsync(
            EndpointUrl.Passkeys.AuthenticationCompletionsV1,
            completionRequest));
        replayedAuthentication.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await AssertRedisKeyMissingAsync($"authentication:passkey-sign-in:{authenticationOptions.FlowToken}");
        await AssertRedisKeyExistsAsync($"authentication:passkey-sign-in-marker:{authenticationOptions.FlowToken}");
    }

    private void ConfigurePasskeyHandler()
    {
        using var scope = CreateScope();
        _passkeyState = scope.ServiceProvider.GetRequiredService<MockPasskeyState>();
        _passkey = new UserPasskeyInfo(
            _credentialId, [2, 3], TimeProvider.System.GetUtcNow(), 1, ["internal"], true, false, false, [4], [5]);
        _passkeyState.UserId = _user.Id;
        _passkeyState.Passkey = _passkey;
    }

    private async Task<SignInResponse> SignInAsync()
    {
        var response = Track(await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password)));
        response.IsSuccessStatusCode.ShouldBeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SignInResponse>())!;
    }

    private async Task<PasskeyOptionsResponse> CreateRegistrationOptionsAsync()
    {
        var response = Track(await Client.PostAsJsonAsync(
            EndpointUrl.Passkeys.RegistrationOptionsV1,
            new CreatePasskeyRegistrationOptionsRequest("Laptop")));
        response.IsSuccessStatusCode.ShouldBeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PasskeyOptionsResponse>())!;
    }

    private async Task<PasskeyOptionsResponse> CreateAuthenticationOptionsAsync()
    {
        var response = Track(await Client.PostAsync(EndpointUrl.Passkeys.AuthenticationOptionsV1, null));
        response.IsSuccessStatusCode.ShouldBeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PasskeyOptionsResponse>())!;
    }

    private async Task CreateVerifiedUserAsync()
    {
        _email = WebApiIntegrationTestData.Email();
        using var scope = CreateScope();
        var countries = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var stakeholderTypes = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholders = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var identity = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var country = (await countries.ListAsync(new FirstCountrySpecification())).FirstOrDefault();
        if (country is null)
        {
            country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
            await countryRepository.AddAsync(country);
            _createdCountry = true;
        }
        _countryId = country.Id;

        _user = AppUser.Create(_email);
        (await identity.CreateAsync(_user, Password)).Succeeded.ShouldBeTrue();
        _user.MarkEmailVerified();
        (await identity.UpdateAsync(_user)).Succeeded.ShouldBeTrue();

        var type = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(_user.Id, _tenantId, country.Id, type.Id, "Jane", "Doe");
        await stakeholderTypes.AddAsync(type);
        await stakeholders.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = type.Id;
    }

    private async Task<IReadOnlyList<IdentityUserPasskey<Guid>>> GetPersistedPasskeysAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Set<IdentityUserPasskey<Guid>>()
            .AsNoTracking()
            .Where(passkey => passkey.UserId == _user.Id)
            .ToListAsync();
    }

    private async Task<int> GetSessionCountAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.AuthenticationSessions.CountAsync(session => session.StakeholderId == _stakeholderId);
    }

    private async Task AssertRedisKeyExistsAsync(string key)
    {
        using var scope = CreateScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        (await redis.GetDatabase().KeyExistsAsync(key)).ShouldBeTrue();
    }

    private async Task AssertRedisKeyMissingAsync(string key)
    {
        using var scope = CreateScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        (await redis.GetDatabase().KeyExistsAsync(key)).ShouldBeFalse();
    }

    private async Task DeleteTestDataAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholders = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var types = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countries = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var stakeholder = await stakeholders.GetByIdAsync(_stakeholderId);
        if (stakeholder is not null) stakeholders.Remove(stakeholder);
        var type = await types.GetByIdAsync(_stakeholderTypeId);
        if (type is not null) types.Remove(type);
        if (_createdCountry)
        {
            var country = await countries.GetByIdAsync(_countryId);
            if (country is not null) countries.Remove(country);
        }
        var user = await users.GetByEmailAsync(_email);
        if (user is not null) users.Remove(user);
        await unitOfWork.SaveChangesAsync();
    }

    private HttpResponseMessage Track(HttpResponseMessage response)
    {
        _responses.Add(response);
        return response;
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }

    private sealed class MockPasskeyState
    {
        public Guid UserId { get; set; }
        public UserPasskeyInfo Passkey { get; set; } = default!;
    }

    private sealed class MockPasskeyHandler(
        UserManager<AppUser> userManager,
        MockPasskeyState state) : IPasskeyHandler<AppUser>
    {

        public Task<PasskeyCreationOptionsResult> MakeCreationOptionsAsync(
            PasskeyUserEntity userEntity,
            HttpContext httpContext) => Task.FromResult(new PasskeyCreationOptionsResult
            {
                CreationOptionsJson = "{\"challenge\":\"challenge\",\"rp\":{\"id\":\"localhost\",\"name\":\"localhost\"}}",
                AttestationState = "attestation-state"
            });

        public Task<PasskeyAttestationResult> PerformAttestationAsync(PasskeyAttestationContext context) =>
            Task.FromResult(PasskeyAttestationResult.Success(state.Passkey, new PasskeyUserEntity
            {
                Id = state.UserId.ToString("D"),
                Name = state.UserId.ToString("D"),
                DisplayName = state.UserId.ToString("D")
            }));

        public Task<PasskeyRequestOptionsResult> MakeRequestOptionsAsync(AppUser? user, HttpContext httpContext) =>
            Task.FromResult(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = "{\"challenge\":\"challenge\",\"rpId\":\"localhost\"}",
                AssertionState = "assertion-state"
            });

        public async Task<PasskeyAssertionResult<AppUser>> PerformAssertionAsync(PasskeyAssertionContext context)
        {
            var user = await userManager.FindByIdAsync(state.UserId.ToString("D"));
            return PasskeyAssertionResult.Success(state.Passkey, user!);
        }
    }
}
