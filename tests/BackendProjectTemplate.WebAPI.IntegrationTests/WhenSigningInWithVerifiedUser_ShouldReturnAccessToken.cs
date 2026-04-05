using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningInWithVerifiedUser_ShouldReturnAccessToken(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private const string FirstName = "Linus";
    private const string LastName = "Torvalds";

    private string _email = string.Empty;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await GivenAVerifiedUserAsync();
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
        SignInResponse? payload = default;

        await WhenSigningIn();
        ThenAnAccessTokenIsReturned();

        async Task WhenSigningIn()
        {
            _response = await Client.PostAsJsonAsync("/api/authentication/sign-in", new SignInRequest
            {
                Email = _email,
                Password = Password
            });

            payload = await _response.Content.ReadFromJsonAsync<SignInResponse>();
        }

        void ThenAnAccessTokenIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            string.IsNullOrWhiteSpace(payload?.AccessToken).ShouldBeFalse();
        }
    }

    private async Task GivenAVerifiedUserAsync()
    {
        _email = $"signin-{Guid.NewGuid():N}@example.com";

        using var signUpResponse = await Client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
        {
            Email = _email,
            Password = Password,
            ConfirmPassword = Password,
            FirstName = FirstName,
            LastName = LastName
        });

        signUpResponse.EnsureSuccessStatusCode();

        var otp = OtpDeliveryService.GetCode(_email) ?? throw new InvalidOperationException("Expected an OTP code to be generated.");

        using var verifyResponse = await Client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
        {
            Email = _email,
            Otp = otp
        });

        verifyResponse.EnsureSuccessStatusCode();
    }
}
