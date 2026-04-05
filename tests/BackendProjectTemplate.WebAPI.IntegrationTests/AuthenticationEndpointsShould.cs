using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class AuthenticationEndpointsShould(ContainersFixture fixture) : WebApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task WhenSigningUp_ShouldReturnAcceptedAndStoreOtp()
    {
        const string password = "P@ssw0rd123!";
        const string firstName = "Ada";
        const string lastName = "Lovelace";

        var email = string.Empty;
        HttpResponseMessage response = default!;

        GivenANewEmailAddress();
        await WhenSigningUp();
        ThenTheRequestIsAcceptedAndOtpIsStored();

        void GivenANewEmailAddress()
        {
            email = $"signup-{Guid.NewGuid():N}@example.com";
        }

        async Task WhenSigningUp()
        {
            response = await Client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                FirstName = firstName,
                LastName = lastName
            });
        }

        void ThenTheRequestIsAcceptedAndOtpIsStored()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            OtpDeliveryService.GetCode(email).ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task WhenVerifyingOtp_ShouldActivateTheAccount()
    {
        const string password = "P@ssw0rd123!";
        const string firstName = "Grace";
        const string lastName = "Hopper";

        var email = string.Empty;
        HttpResponseMessage verifyResponse = default!;

        await GivenASignedUpUser();
        await WhenVerifyingOtp();
        ThenTheAccountIsActivated();

        async Task GivenASignedUpUser()
        {
            email = $"verify-{Guid.NewGuid():N}@example.com";

            await Client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                FirstName = firstName,
                LastName = lastName
            });
        }

        async Task WhenVerifyingOtp()
        {
            verifyResponse = await Client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
            {
                Email = email,
                Otp = OtpDeliveryService.GetCode(email)!
            });
        }

        void ThenTheAccountIsActivated()
        {
            verifyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task WhenSigningInAfterVerification_ShouldReturnAccessToken()
    {
        var email = string.Empty;
        const string password = "P@ssw0rd123!";
        const string firstName = "Linus";
        const string lastName = "Torvalds";
        HttpResponseMessage signInResponse = default!;
        SignInResponse? payload = default;

        await GivenAVerifiedUser();
        await WhenSigningIn();
        ThenAnAccessTokenIsReturned();

        async Task GivenAVerifiedUser()
        {
            email = $"signin-{Guid.NewGuid():N}@example.com";

            await Client.PostAsJsonAsync("/api/authentication/sign-up", new SignUpRequest
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                FirstName = firstName,
                LastName = lastName
            });

            await Client.PostAsJsonAsync("/api/authentication/sign-up/otp", new SignUpOtpRequest
            {
                Email = email,
                Otp = OtpDeliveryService.GetCode(email)!
            });
        }

        async Task WhenSigningIn()
        {
            signInResponse = await Client.PostAsJsonAsync("/api/authentication/sign-in", new SignInRequest
            {
                Email = email,
                Password = password
            });

            payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();
        }

        void ThenAnAccessTokenIsReturned()
        {
            signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            string.IsNullOrWhiteSpace(payload?.AccessToken).ShouldBeFalse();
        }
    }
}
