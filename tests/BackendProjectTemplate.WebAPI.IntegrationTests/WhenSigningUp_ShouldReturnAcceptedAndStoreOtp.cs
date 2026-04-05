using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Domain.Authentication.Persistence;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.WebAPI;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningUp_ShouldReturnAcceptedAndStoreOtp(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private const string FirstName = "Ada";
    private const string LastName = "Lovelace";

    private string _email = string.Empty;
    private SignUpRequest _request = default!;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        ClearOtpDeliveries();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task Verify()
    {
        GivenANewEmailAddress();
        await WhenSigningUp();
        ThenTheRequestIsAcceptedAndOtpIsStored();

        void GivenANewEmailAddress()
        {
            _email = $"signup-{Guid.NewGuid():N}@example.com";
            _request = new SignUpRequest
            {
                Email = _email,
                Password = Password,
                ConfirmPassword = Password,
                FirstName = FirstName,
                LastName = LastName
            };
        }

        async Task WhenSigningUp()
        {
            _response = await Client.PostAsJsonAsync(EndpointUrl.SignUp.V1, _request);
        }

        void ThenTheRequestIsAcceptedAndOtpIsStored()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            OtpDeliveryService.GetCode(_email).ShouldNotBeNull();
        }
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
