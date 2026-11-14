using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.Features.Payments.Providers;
using BackendProjectTemplate.WebAPI.Features.Providers;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Authorization;

[Collection(nameof(ContainersCollection))]
public sealed class When_AuthorizingAdministrativeEndpoints_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private readonly List<HttpResponseMessage> _responses = [];
    private AppUser _user = default!;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private string _email = string.Empty;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        var tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        await CreateVerifiedUserAsync(tenantId);
    }

    public async Task DisposeAsync()
    {
        foreach (var response in _responses)
        {
            response.Dispose();
        }

        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var stakeholder = await stakeholderRepository.GetByIdAsync(_stakeholderId);
        var stakeholderType = await stakeholderTypeRepository.GetByIdAsync(_stakeholderTypeId);
        var country = await countryRepository.GetByIdAsync(_countryId);
        var user = await userRepository.GetByEmailAsync(_email);
        if (stakeholder is not null) stakeholderRepository.Remove(stakeholder);
        if (stakeholderType is not null) stakeholderTypeRepository.Remove(stakeholderType);
        if (country is not null) countryRepository.Remove(country);
        if (user is not null) userRepository.Remove(user);
        await unitOfWork.SaveChangesAsync();

        await DisposeClientAsync();
    }

    [Fact]
    public async Task EnforceTheRolePermissionMatrixAndCurrentRoleMembership()
    {
        (await ActivateProviderAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await AuthenticateAsync();

        (await ActivateProviderAsync()).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ActivatePaymentProviderAsync()).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await AssertRoleAsync(
            ApplicationRoles.ComplianceOfficer,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Forbidden);
        await AssertRoleAsync(
            ApplicationRoles.SupportAgent,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Forbidden);
        await AssertRoleAsync(
            ApplicationRoles.FinanceOfficer,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);
        await AssertRoleAsync(
            ApplicationRoles.Administrator,
            HttpStatusCode.NotFound,
            HttpStatusCode.NotFound);

        (await ActivateProviderAsync()).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ActivatePaymentProviderAsync()).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task AssertRoleAsync(
        string role,
        HttpStatusCode expectedProviderStatus,
        HttpStatusCode expectedPaymentProviderStatus)
    {
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(_user.Id.ToString());
        user.ShouldNotBeNull();
        (await userManager.AddToRoleAsync(user, role)).Succeeded.ShouldBeTrue();

        (await ActivateProviderAsync()).StatusCode.ShouldBe(expectedProviderStatus);
        (await ActivatePaymentProviderAsync()).StatusCode.ShouldBe(expectedPaymentProviderStatus);

        (await userManager.RemoveFromRoleAsync(user, role)).Succeeded.ShouldBeTrue();
    }

    private async Task<HttpResponseMessage> ActivateProviderAsync()
    {
        var response = await Client.PutAsJsonAsync(
            $"{EndpointUrl.Providers.V1}/active",
            new ActivateProviderRequest("Email", $"missing-{Guid.CreateVersion7():N}"));
        _responses.Add(response);
        return response;
    }

    private async Task<HttpResponseMessage> ActivatePaymentProviderAsync()
    {
        var response = await Client.PutAsJsonAsync(
            $"{EndpointUrl.PaymentProviders.V1}/{Guid.CreateVersion7()}/activation",
            new SetPaymentProviderActivationRequest(true));
        _responses.Add(response);
        return response;
    }

    private async Task AuthenticateAsync()
    {
        using var response = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_email, Password));
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync(Guid tenantId)
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();
        _user = AppUser.Create(_email);
        (await identityService.CreateAsync(_user, Password)).Succeeded.ShouldBeTrue();
        _user.MarkEmailVerified();
        (await identityService.UpdateAsync(_user)).Succeeded.ShouldBeTrue();

        var country = Country.Create("Authorization Test Country", "AT", "+1", "https://example.com/at.svg");
        var stakeholderType = StakeholderType.Create(tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(
            _user.Id,
            tenantId,
            country.Id,
            stakeholderType.Id,
            "Ada",
            "Lovelace");
        await countryRepository.AddAsync(country);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();
        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _stakeholderId = stakeholder.Id;
    }
}
