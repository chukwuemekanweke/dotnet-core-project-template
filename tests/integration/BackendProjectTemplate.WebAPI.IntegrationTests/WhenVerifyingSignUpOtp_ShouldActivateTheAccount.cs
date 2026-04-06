using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.WebAPI;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenVerifyingSignUpOtp_ShouldActivateTheAccount(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _otp = string.Empty;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await CreateSignedUpUserAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task Verify()
    {
        await WhenVerifyingOtp();
        ThenTheAccountIsActivated();

        async Task WhenVerifyingOtp()
        {
            _response = await Client.PostAsJsonAsync(EndpointUrl.EmailConfirmations.V1, new SignUpOtpRequest
            {
                Email = _email,
                Otp = _otp
            });
        }

        void ThenTheAccountIsActivated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    private async Task CreateSignedUpUserAsync()
    {
        _email = WebApiIntegrationTestData.Email();
        _firstName = WebApiIntegrationTestData.FirstName();
        _lastName = WebApiIntegrationTestData.LastName();
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var user = AppUser.Create(_email, _firstName, _lastName, timeProvider.GetUtcNow());
        var createResult = await identityService.CreateAsync(user, Password);
        createResult.Succeeded.ShouldBeTrue();

        _otp = await identityService.GenerateSignUpOtpAsync(user);
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await repository.GetByEmailAsync(_email);

        if (user is not null)
        {
            repository.Remove(user);
            await unitOfWork.SaveChangesAsync();
        }
    }
}
