using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
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
        await DeleteAuthenticationUserByEmailAsync(_email);
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
            _response = await Client.PostAsJsonAsync("/api/authentication/sign-up", _request);
        }

        void ThenTheRequestIsAcceptedAndOtpIsStored()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            OtpDeliveryService.GetCode(_email).ShouldNotBeNull();
        }
    }
}
