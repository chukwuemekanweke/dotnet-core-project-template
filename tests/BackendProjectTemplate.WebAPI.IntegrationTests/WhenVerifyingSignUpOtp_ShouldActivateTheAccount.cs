using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenVerifyingSignUpOtp_ShouldActivateTheAccount(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private const string FirstName = "Grace";
    private const string LastName = "Hopper";

    private string _email = string.Empty;
    private string _otp = string.Empty;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await GivenASignedUpUserAsync();
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
        await WhenVerifyingOtp();
        ThenTheAccountIsActivated();

        async Task WhenVerifyingOtp()
        {
            _response = await Client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
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

    private async Task GivenASignedUpUserAsync()
    {
        _email = $"verify-{Guid.NewGuid():N}@example.com";

        using var signUpResponse = await Client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
        {
            Email = _email,
            Password = Password,
            ConfirmPassword = Password,
            FirstName = FirstName,
            LastName = LastName
        });

        signUpResponse.EnsureSuccessStatusCode();
        _otp = OtpDeliveryService.GetCode(_email) ?? throw new InvalidOperationException("Expected an OTP code to be generated.");
    }
}
